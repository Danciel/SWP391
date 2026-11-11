using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    // C6: the Owner says "I'm alive" after the DMS was triggered.
    public interface IOwnerAliveService
    {
        Task<OwnerAliveResponse> ConfirmAliveAsync(
            Guid willId,
            Guid ownerAccountId,
            CancellationToken ct = default);
    }
}
