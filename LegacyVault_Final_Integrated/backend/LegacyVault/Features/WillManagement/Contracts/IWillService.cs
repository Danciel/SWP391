using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IWillService
    {
        Task<WillSummaryResponse> CreateAsync(
            CreateWillRequest request);

        Task<List<WillSummaryResponse>> GetMineAsync();

        Task<WillDetailResponse> GetByIdAsync(Guid willId);

        Task<WillSummaryResponse> UpdateAsync(
            Guid willId,
            UpdateWillRequest request);

        Task DeleteAsync(Guid willId);

        Task<WillAssetResponse> AddAssetAsync(
            Guid willId,
            AddWillAssetRequest request);

        Task<BeneficiaryResponse> AddBeneficiaryAsync(
            Guid willId,
            CreateBeneficiaryRequest request);

        Task<AssetAssignmentResponse> AssignAssetAsync(
            Guid willId,
            CreateAssetAssignmentRequest request);

        Task<List<ExecutorListItemResponse>> GetExecutorsAsync();

        Task<ExecutorAssignmentResponse> AssignExecutorAsync(
            Guid willId,
            AssignExecutorRequest request);

        Task<DmsConfigurationResponse> ConfigureDmsAsync(
            Guid willId,
            ConfigureDmsRequest request);

        Task<DmsConfigurationResponse> CheckInAsync(Guid willId);

        Task<WillDetailResponse> SubmitAsync(Guid willId);
    }
}
