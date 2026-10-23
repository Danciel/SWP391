using LegacyVault.Models;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IWillRepository
    {
        Task AddWillAsync(Will will);

        Task<List<Will>> GetOwnedWillsAsync(Guid ownerAccountId);

        Task<Will?> GetOwnedWillAsync(
            Guid willId,
            Guid ownerAccountId,
            bool includeDetails = false);

        Task<Asset?> GetOwnedAssetAsync(
            Guid assetId,
            Guid ownerAccountId);

        Task<Account?> GetAccountAsync(Guid accountId);

        Task<ExecutorProfile?> GetExecutorProfileAsync(
            Guid accountId);

        Task<List<ExecutorProfile>> GetVerifiedExecutorsAsync();

        Task<WillAsset?> GetWillAssetAsync(Guid willAssetId);

        Task<BeneficiaryDesignation?> GetBeneficiaryAsync(
            Guid beneficiaryId);

        Task<bool> AssignmentExistsAsync(
            Guid willAssetId,
            Guid beneficiaryDesignationId);

        Task<ExecutorAssignment?> GetExecutorAssignmentAsync(
            Guid willId);

        Task<DmsConfiguration?> GetDmsConfigurationAsync(
            Guid willId);

        Task AddWillAssetAsync(WillAsset willAsset);

        Task AddBeneficiaryAsync(
            BeneficiaryDesignation beneficiary);

        Task AddAssetAssignmentAsync(
            AssetAssignment assignment);

        Task<bool> TryAddAssetAssignmentWithinLimitAsync(AssetAssignment assignment);

        Task<bool> TryUpdateAssetAllocationWithinLimitAsync(AssetAssignment assignment, decimal newPercentage);

        Task AddExecutorAssignmentAsync(
            ExecutorAssignment assignment);

        Task AddDmsConfigurationAsync(
            DmsConfiguration configuration);

        Task AddDmsCheckInAsync(DmsCheckIn checkIn);

        // W02: atomically check the exclusive lock and insert.
        // Returns the Id of the Will that already holds the Asset,
        // or null when the insert succeeded.
        Task<Guid?> TryAddWillAssetExclusiveAsync(
            WillAsset willAsset);

        Task<List<Guid>> GetLockedAssetIdsAsync(
            Guid ownerAccountId);

        Task RemoveWillAssetAsync(WillAsset willAsset);

        Task RemoveAssignmentAsync(AssetAssignment assignment);

        void RemoveWill(Will will);

        // AD09: audit rows are saved together with the business change.
        void AddAuditLog(AuditLog log);

        Task SaveChangesAsync();
    }
}
