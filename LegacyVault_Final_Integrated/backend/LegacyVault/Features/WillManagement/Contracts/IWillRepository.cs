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

        Task AddExecutorAssignmentAsync(
            ExecutorAssignment assignment);

        Task AddDmsConfigurationAsync(
            DmsConfiguration configuration);

        Task AddDmsCheckInAsync(DmsCheckIn checkIn);

        void RemoveWill(Will will);

        Task SaveChangesAsync();
    }
}
