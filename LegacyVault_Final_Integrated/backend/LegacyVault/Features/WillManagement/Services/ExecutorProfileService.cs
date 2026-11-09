using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    public class ExecutorProfileService : IExecutorProfileService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly ICurrentUserService _user;

        public ExecutorProfileService(
            LegacyVaultDbContext db,
            ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public async Task<ExecutorProfileResponse> UpdateMyProfileAsync(
            UpdateExecutorProfileRequest request)
        {
            var me = _user.GetRequiredAccountId();

            var profile = await _db.ExecutorProfiles
                .FirstOrDefaultAsync(x => x.AccountId == me)
                ?? throw new InvalidOperationException(
                    "Only an Executor can edit an executor profile.");

            // CompletedCases / Rating are system-managed, never edited here.
            profile.Bio = string.IsNullOrWhiteSpace(request.Bio)
                ? null : request.Bio.Trim();
            profile.Specialty = string.IsNullOrWhiteSpace(request.Specialty)
                ? null : request.Specialty.Trim();
            profile.ExperienceYears = request.ExperienceYears;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = me, Action = "EXECUTOR_PROFILE_UPDATED",
                EntityType = "ExecutorProfile", EntityId = me,
                Details = System.Text.Json.JsonSerializer.Serialize(new { profile.Specialty, profile.ExperienceYears }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync();
            return await GetProfileAsync(me);
        }

        public async Task<ExecutorPricingReferenceResponse>
            GetPricingReferenceAsync()
        {
            const int minimumSample = 3;
            const string currency = "VND";

            var rows = await _db.ExecutorProfiles
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x => x.IsVerified && !x.IsSuspended &&
                            x.ServiceFee > 0m && x.Currency == currency)
                .ToListAsync();

            var fees = rows.Select(x => x.ServiceFee).OrderBy(x => x).ToList();
            var enough = fees.Count >= minimumSample;

            decimal? median = null;
            if (enough)
            {
                var mid = fees.Count / 2;
                median = fees.Count % 2 == 1
                    ? fees[mid]
                    : (fees[mid - 1] + fees[mid]) / 2m;
            }

            // Fees that Owners really paid in the mock wallet (completed only).
            var paid = await _db.ServicePayments
                .AsNoTracking()
                .Where(x => x.Status == "RELEASED" && x.Currency == currency)
                .Select(x => x.Amount)
                .ToListAsync();

            return new ExecutorPricingReferenceResponse
            {
                Currency = currency,
                Source = "Fees advertised by verified, non-suspended executors on this platform, plus completed (RELEASED) mock payments.",
                Disclaimer = "Demo data only. This is not a real market price and must not be treated as one.",
                HasReferenceData = enough,
                MinimumSampleSize = minimumSample,
                SampleSize = fees.Count,
                Minimum = enough ? fees.First() : null,
                Median = median,
                Average = enough ? Math.Round(fees.Average(), 2) : null,
                Maximum = enough ? fees.Last() : null,
                CompletedPaymentCount = paid.Count,
                CompletedPaymentAverage = paid.Count > 0
                    ? Math.Round(paid.Average(), 2)
                    : null,
                Executors = rows
                    .OrderBy(x => x.ServiceFee)
                    .Select(x => new ExecutorPricingComparisonItem
                    {
                        AccountId = x.AccountId,
                        FullName = x.Account != null ? x.Account.FullName : string.Empty,
                        ServiceFee = x.ServiceFee,
                        Rating = x.Rating,
                        CompletedCases = x.CompletedCases,
                        DifferenceFromMedianPercent =
                            median.HasValue && median.Value > 0m
                                ? Math.Round((x.ServiceFee - median.Value) / median.Value * 100m, 1)
                                : null
                    })
                    .ToList()
            };
        }

        public async Task<ExecutorProfileResponse> GetProfileAsync(
            Guid accountId)
        {
            var profile = await _db.ExecutorProfiles
                .AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.AccountId == accountId)
                ?? throw new KeyNotFoundException("Executor not found.");

            var reviewCount = await _db.ExecutorReviews
                .CountAsync(x => x.ExecutorAccountId == accountId);

            return new ExecutorProfileResponse
            {
                AccountId = profile.AccountId,
                FullName = profile.Account?.FullName ?? string.Empty,
                Bio = profile.Bio,
                Specialty = profile.Specialty,
                ExperienceYears = profile.ExperienceYears,
                CompletedCases = profile.CompletedCases,
                Rating = profile.Rating,
                ReviewCount = reviewCount,
                ServiceFee = profile.ServiceFee,
                Currency = profile.Currency,
                IsVerified = profile.IsVerified,
                IsSuspended = profile.IsSuspended,
                RecentReviews = (await GetReviewsAsync(accountId))
                    .Take(5).ToList()
            };
        }

        public async Task<List<ExecutorReviewResponse>> GetReviewsAsync(
            Guid accountId)
        {
            return await _db.ExecutorReviews
                .AsNoTracking()
                .Where(x => x.ExecutorAccountId == accountId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ExecutorReviewResponse
                {
                    Id = x.Id,
                    WillId = x.WillId,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    OwnerName = x.OwnerAccount != null
                        ? x.OwnerAccount.FullName : string.Empty,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ExecutorReviewResponse> CreateReviewAsync(
            Guid executorAccountId,
            CreateExecutorReviewRequest request)
        {
            var me = _user.GetRequiredAccountId();

            // Only the Owner of a COMPLETED handover handled by this
            // Executor may review it.
            var will = await _db.Wills
                .AsNoTracking()
                .Include(x => x.ExecutorAssignment)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.WillId &&
                    x.OwnerAccountId == me)
                ?? throw new KeyNotFoundException("Will not found.");

            if (will.Status != "HANDED_OVER" ||
                will.ExecutorAssignment?.ExecutorAccountId != executorAccountId)
            {
                throw new InvalidOperationException(
                    "You can review an executor only after the handover of that will is completed.");
            }

            var review = new ExecutorReview
            {
                Id = Guid.NewGuid(),
                ExecutorAccountId = executorAccountId,
                OwnerAccountId = me,
                WillId = will.Id,
                Rating = request.Rating,
                Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? null : request.Comment.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ExecutorReviews.Add(review);
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = me, Action = "EXECUTOR_REVIEW_CREATED",
                EntityType = "ExecutorReview", EntityId = review.Id,
                Details = System.Text.Json.JsonSerializer.Serialize(new { review.WillId, review.ExecutorAccountId, review.Rating }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new WillConflictException(
                    "This will has already been reviewed.");
            }

            var avg = await _db.ExecutorReviews
                .Where(x => x.ExecutorAccountId == executorAccountId)
                .AverageAsync(x => (decimal)x.Rating);

            var rounded = Math.Round(avg, 2);
            await _db.ExecutorProfiles
                .Where(x => x.AccountId == executorAccountId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Rating, rounded));

            var ownerName = await _db.Accounts
                .Where(x => x.Id == me)
                .Select(x => x.FullName)
                .FirstAsync();

            return new ExecutorReviewResponse
            {
                Id = review.Id,
                WillId = review.WillId,
                Rating = review.Rating,
                Comment = review.Comment,
                OwnerName = ownerName,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
