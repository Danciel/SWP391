using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IWalletService
    {
        Task<WalletResponse> GetMineAsync();

        Task<WalletResponse> TopUpAsync(decimal amount, string idempotencyKey);

        // Used by the Will / Handover flows (not exposed as endpoints).
        Task HoldFeeAsync(
            Guid willId,
            Guid ownerAccountId,
            Guid executorAccountId,
            decimal amount,
            string currency);

        // An OPEN payment dispute blocks release unless the admin is
        // resolving that very dispute (ignoreOpenDispute = true).
        Task ReleaseFeeAsync(Guid willId, bool ignoreOpenDispute = false);

        Task RefundFeeAsync(Guid willId);
    }
}
