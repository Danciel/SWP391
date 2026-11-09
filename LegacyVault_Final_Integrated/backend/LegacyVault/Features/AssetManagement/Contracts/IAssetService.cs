using LegacyVault.Features.AssetManagement.DTOs;

namespace LegacyVault.Features.AssetManagement.Contracts
{
    public interface IAssetService
    {
        Task<AssetResponse> CreateAsync(CreateAssetRequest request);

        Task<List<AssetResponse>> GetMineAsync();

        Task<AssetResponse> GetByIdAsync(Guid assetId);

        Task<AssetResponse> UpdateAsync(
            Guid assetId,
            UpdateAssetRequest request);

        Task DeleteAsync(Guid assetId);

        Task<AssetSecretResponse> RevealSecretAsync(Guid assetId);

        Task<AssetVerificationResponse> VerifyAsync(Guid assetId);
    }
}
