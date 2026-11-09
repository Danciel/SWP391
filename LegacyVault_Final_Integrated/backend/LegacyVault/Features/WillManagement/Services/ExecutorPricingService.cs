using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    public class ExecutorPricingService : IExecutorPricingService
    {
        private const int YearsCap = 40;
        private const int CasesCap = 200;
        private const decimal AtThresholdPercent = 5m;

        private readonly LegacyVaultDbContext _db;

        public ExecutorPricingService(LegacyVaultDbContext db)
        {
            _db = db;
        }

        public async Task<ExecutorPricingRule> GetRulesAsync()
        {
            var row = await _db.ExecutorPricingRules.AsNoTracking()
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();
            return row ?? ExecutorPricingRule.Default();
        }

        public async Task<ExecutorPricingRuleResponse> GetRulesResponseAsync()
        {
            var rules = await GetRulesAsync();
            return ToResponse(rules);
        }

        public async Task<ExecutorPricingRuleResponse> UpdateRulesAsync(
            Guid adminAccountId, UpdateExecutorPricingRuleRequest request)
        {
            if (request.MinFee > request.MaxFee)
                throw new InvalidOperationException("Minimum fee cannot be larger than maximum fee.");

            var row = await _db.ExecutorPricingRules
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();

            if (row == null)
            {
                row = new ExecutorPricingRule { Id = Guid.NewGuid() };
                _db.ExecutorPricingRules.Add(row);
            }

            row.BaseFee = request.BaseFee;
            row.PerExperienceYear = request.PerExperienceYear;
            row.PerStar = request.PerStar;
            row.PerCompletedCase = request.PerCompletedCase;
            row.WinRateBonus = request.WinRateBonus;
            row.MinFee = request.MinFee;
            row.MaxFee = request.MaxFee;
            row.RoundTo = request.RoundTo;
            row.Currency = request.Currency.Trim().ToUpperInvariant();
            row.UpdatedAt = DateTimeOffset.UtcNow;
            row.UpdatedByAccountId = adminAccountId;

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = adminAccountId,
                Action = "EXECUTOR_PRICING_RULES_UPDATED",
                EntityType = "ExecutorPricingRule",
                EntityId = row.Id,
                Details = System.Text.Json.JsonSerializer.Serialize(request),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync();
            return ToResponse(row);
        }

        public async Task EnrichListAsync(List<ExecutorListItemResponse> items)
        {
            if (items.Count == 0) return;

            var ids = items.Select(x => x.AccountId).ToList();
            var profiles = await _db.ExecutorProfiles.AsNoTracking()
                .Where(x => ids.Contains(x.AccountId))
                .ToDictionaryAsync(x => x.AccountId);
            var rules = await GetRulesAsync();

            foreach (var item in items)
            {
                if (profiles.TryGetValue(item.AccountId, out var profile))
                    Apply(item, BuildSuggestion(rules, profile), profile.ServiceFee, profile.Currency, rules.Currency);
            }
        }

        public async Task EnrichProfileAsync(ExecutorProfileResponse response)
        {
            var profile = await _db.ExecutorProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == response.AccountId);
            if (profile == null) return;

            var rules = await GetRulesAsync();
            Apply(response, BuildSuggestion(rules, profile), profile.ServiceFee, profile.Currency, rules.Currency);
        }

        public async Task<ExecutorPricingSuggestionResponse> GetMineAsync(Guid executorAccountId)
        {
            var profile = await _db.ExecutorProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == executorAccountId)
                ?? throw new InvalidOperationException("Executor profile was not found.");

            var rules = await GetRulesAsync();
            var s = BuildSuggestion(rules, profile);

            var response = new ExecutorPricingSuggestionResponse
            {
                Currency = rules.Currency,
                CurrentFee = profile.ServiceFee,
                Components = s.Components,
                MinFee = rules.MinFee,
                MaxFee = rules.MaxFee,
                Clamped = s.Clamped
            };
            Apply(response, s, profile.ServiceFee, profile.Currency, rules.Currency);
            return response;
        }

        // ---------------------------------------------------------------

        private sealed class Suggestion
        {
            public decimal Fee { get; set; }
            public bool Clamped { get; set; }
            public List<SuggestionComponent> Components { get; set; } = new();
            public int Years { get; set; }
            public decimal Rating { get; set; }
            public int Cases { get; set; }
            public int? Won { get; set; }
            public decimal? WinRate { get; set; }
            public bool FromCsv { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
            public int? EvalYears { get; set; }
            public decimal? EvalRating { get; set; }
        }

        private static Suggestion BuildSuggestion(ExecutorPricingRule r, ExecutorProfile p)
        {
            bool fromCsv = p.EvalSource == "CSV";

            int years = fromCsv && p.EvalExperienceYears.HasValue ? p.EvalExperienceYears.Value : p.ExperienceYears;
            decimal rating = fromCsv && p.EvalRating.HasValue ? p.EvalRating.Value : p.Rating;
            int cases = fromCsv && p.EvalTotalCases.HasValue ? p.EvalTotalCases.Value : p.CompletedCases;
            int? won = fromCsv ? p.EvalWonCases : null;
            decimal? winRate = won.HasValue && cases > 0
                ? Math.Min(1m, (decimal)won.Value / cases)
                : null;

            var parts = new List<SuggestionComponent>
            {
                new() { Label = "Base fee", Amount = r.BaseFee, Detail = "Starting point for every Executor" },
                new()
                {
                    Label = "Experience",
                    Amount = r.PerExperienceYear * Math.Min(years, YearsCap),
                    Detail = $"{years} year(s) x {r.PerExperienceYear:N0}"
                },
                new()
                {
                    Label = "Rating",
                    Amount = r.PerStar * rating,
                    Detail = $"{rating:0.0} star(s) x {r.PerStar:N0}"
                },
                new()
                {
                    Label = "Completed cases",
                    Amount = r.PerCompletedCase * Math.Min(cases, CasesCap),
                    Detail = $"{cases} case(s) x {r.PerCompletedCase:N0}"
                }
            };

            if (winRate.HasValue)
            {
                parts.Add(new SuggestionComponent
                {
                    Label = "Win rate",
                    Amount = r.WinRateBonus * winRate.Value,
                    Detail = $"{winRate.Value * 100:0}% x {r.WinRateBonus:N0}"
                });
            }

            decimal raw = parts.Sum(x => x.Amount);
            decimal fee = Math.Min(Math.Max(raw, r.MinFee), r.MaxFee);
            bool clamped = fee != raw;

            if (r.RoundTo > 0m)
                fee = Math.Round(fee / r.RoundTo, MidpointRounding.AwayFromZero) * r.RoundTo;

            return new Suggestion
            {
                Fee = fee,
                Clamped = clamped,
                Components = parts,
                Years = years,
                Rating = rating,
                Cases = cases,
                Won = won,
                WinRate = winRate,
                FromCsv = fromCsv,
                UpdatedAt = fromCsv ? p.EvalUpdatedAt : null,
                EvalYears = fromCsv ? p.EvalExperienceYears : null,
                EvalRating = fromCsv ? p.EvalRating : null
            };
        }

        private static void Apply(
            FeeSuggestionFields target, Suggestion s,
            decimal askingFee, string askingCurrency, string ruleCurrency)
        {
            target.EvaluationSource = s.FromCsv ? "CSV" : "PROFILE";
            target.EvaluationUpdatedAt = s.UpdatedAt;
            target.EvalExperienceYears = s.EvalYears;
            target.EvalRating = s.EvalRating;
            target.TotalCases = s.FromCsv ? s.Cases : null;
            target.WonCases = s.Won;
            target.WinRatePercent = s.WinRate.HasValue ? Math.Round(s.WinRate.Value * 100m, 1) : null;

            // A suggestion is only comparable when both sides use the same currency.
            if (!string.Equals(askingCurrency, ruleCurrency, StringComparison.OrdinalIgnoreCase))
                return;

            target.SuggestedFee = s.Fee;

            if (askingFee > 0m && s.Fee > 0m)
            {
                var diff = Math.Round((askingFee - s.Fee) / s.Fee * 100m, 1);
                target.FeeDifferencePercent = diff;
                target.FeePosition = diff > AtThresholdPercent ? "ABOVE"
                    : diff < -AtThresholdPercent ? "BELOW"
                    : "AT";
            }
        }

        private static ExecutorPricingRuleResponse ToResponse(ExecutorPricingRule r) => new()
        {
            BaseFee = r.BaseFee,
            PerExperienceYear = r.PerExperienceYear,
            PerStar = r.PerStar,
            PerCompletedCase = r.PerCompletedCase,
            WinRateBonus = r.WinRateBonus,
            MinFee = r.MinFee,
            MaxFee = r.MaxFee,
            RoundTo = r.RoundTo,
            Currency = r.Currency,
            UpdatedAt = r.UpdatedAt,
            IsDefault = r.Id == Guid.Empty
        };
    }
}
