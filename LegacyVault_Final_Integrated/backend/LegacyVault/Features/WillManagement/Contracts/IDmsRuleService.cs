using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IDmsRuleService
    {
        // Admin read (null if not configured yet).
        Task<DmsRuleResponse?> GetAsync(CancellationToken ct = default);

        // Admin write — throws InvalidOperationException on validation or already-exists.
        Task<DmsRuleResponse> CreateAsync(
            UpsertDmsRuleRequest request,
            Guid adminId,
            CancellationToken ct = default);

        Task<DmsRuleResponse> UpdateAsync(
            UpsertDmsRuleRequest request,
            Guid adminId,
            CancellationToken ct = default);

        // Admin reset — throws KeyNotFoundException when absent.
        Task DeleteAsync(CancellationToken ct = default);

        // Internal / owner read. Returns the entity so WillService can
        // also snapshot GraceDays (hidden from the owner-facing endpoint).
        Task<DmsRule?> GetEffectiveAsync(CancellationToken ct = default);
    }
}