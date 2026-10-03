using SWP301BETASK.Features.AssetManagement.DTOs;

namespace SWP301BETASK.Features.AssetManagement.Contracts
{
    public interface IAssetService
    {
        Task<List<AssetResponse>> GetAllAsync(
            Guid accountId);

        Task<AssetResponse?> GetByIdAsync(
            Guid accountId,
            Guid assetId);

        Task<AssetSecretResponse?> GetSecretAsync(
            Guid accountId,
            Guid assetId);

        Task<AssetResponse> CreateAsync(
            Guid accountId,
            CreateAssetRequest request);

        Task<AssetResponse?> UpdateAsync(
            Guid accountId,
            Guid assetId,
            UpdateAssetRequest request);

        Task<bool> DeleteAsync(
            Guid accountId,
            Guid assetId);

        Task<VerifyAssetResponse> VerifyAsync(
            VerifyAssetRequest request);
    }
}