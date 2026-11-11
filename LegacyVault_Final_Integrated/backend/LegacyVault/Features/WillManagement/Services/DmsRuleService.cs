using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;

namespace LegacyVault.Features.WillManagement.Services
{
    public class DmsRuleService : IDmsRuleService
    {
        private readonly IDmsRuleRepository _repo;

        public DmsRuleService(IDmsRuleRepository repo)
        {
            _repo = repo;
        }

        public async Task<DmsRuleResponse?> GetAsync(
            CancellationToken ct = default)
        {
            var rule = await _repo.GetAsync(ct);
            return rule == null ? null : ToResponse(rule);
        }

        public async Task<DmsRuleResponse> CreateAsync(
            UpsertDmsRuleRequest request,
            Guid adminId,
            CancellationToken ct = default)
        {
            if (await _repo.GetAsync(ct) != null)
            {
                throw new InvalidOperationException(
                    "A DMS rule already exists. Use PUT to update it.");
            }

            Validate(request);

            var now = DateTimeOffset.UtcNow;
            var rule = new DmsRule
            {
                Id = DmsRule.SingletonId,
                ReminderBeforeDays = request.ReminderBeforeDays,
                MaxMissedCheckIns = request.MaxMissedCheckIns,
                GraceDays = request.GraceDays,
                MinCheckInIntervalDays = request.MinCheckInIntervalDays,
                MaxCheckInIntervalDays = request.MaxCheckInIntervalDays,
                UpdatedByAccountId = adminId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repo.AddAsync(rule, ct);
            await _repo.SaveChangesAsync(ct);

            return ToResponse(rule);
        }

        public async Task<DmsRuleResponse> UpdateAsync(
            UpsertDmsRuleRequest request,
            Guid adminId,
            CancellationToken ct = default)
        {
            var rule = await _repo.GetAsync(ct)
                ?? throw new KeyNotFoundException(
                    "DMS rule has not been configured yet.");

            Validate(request);

            rule.ReminderBeforeDays = request.ReminderBeforeDays;
            rule.MaxMissedCheckIns = request.MaxMissedCheckIns;
            rule.GraceDays = request.GraceDays;
            rule.MinCheckInIntervalDays = request.MinCheckInIntervalDays;
            rule.MaxCheckInIntervalDays = request.MaxCheckInIntervalDays;
            rule.UpdatedByAccountId = adminId;
            rule.UpdatedAt = DateTimeOffset.UtcNow;

            _repo.Update(rule);
            await _repo.SaveChangesAsync(ct);

            return ToResponse(rule);
        }

        public async Task DeleteAsync(CancellationToken ct = default)
        {
            var rule = await _repo.GetAsync(ct)
                ?? throw new KeyNotFoundException(
                    "DMS rule has not been configured.");

            _repo.Remove(rule);
            await _repo.SaveChangesAsync(ct);
        }

        public Task<DmsRule?> GetEffectiveAsync(
            CancellationToken ct = default) =>
            _repo.GetAsync(ct);

        // Cross-field invariants. ReminderBeforeDays must remain strictly
        // below the smallest interval the owner is allowed to pick, otherwise
        // a valid owner choice (interval == Min) would violate reminder < interval.
        private static void Validate(UpsertDmsRuleRequest request)
        {
            if (request.MinCheckInIntervalDays >
                request.MaxCheckInIntervalDays)
            {
                throw new InvalidOperationException(
                    "MinCheckInIntervalDays must be less than or equal to MaxCheckInIntervalDays.");
            }

            if (request.ReminderBeforeDays >=
                request.MinCheckInIntervalDays)
            {
                throw new InvalidOperationException(
                    "ReminderBeforeDays must be less than MinCheckInIntervalDays.");
            }
        }

        private static DmsRuleResponse ToResponse(DmsRule rule) => new()
        {
            Id = rule.Id,
            ReminderBeforeDays = rule.ReminderBeforeDays,
            MaxMissedCheckIns = rule.MaxMissedCheckIns,
            GraceDays = rule.GraceDays,
            MinCheckInIntervalDays = rule.MinCheckInIntervalDays,
            MaxCheckInIntervalDays = rule.MaxCheckInIntervalDays,
            UpdatedByAccountId = rule.UpdatedByAccountId,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}