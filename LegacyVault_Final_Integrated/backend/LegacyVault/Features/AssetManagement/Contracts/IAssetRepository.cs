using LegacyVault.Models;

namespace LegacyVault.Features.AssetManagement.Contracts
{
    public interface IAssetRepository
    {
        Task AddAsync(Asset asset);

        Task AddSecretAsync(AssetSecret secret);

        Task AddVerificationAsync(AssetVerification verification);

        Task<List<Asset>> GetOwnedAssetsAsync(Guid ownerAccountId);

        Task<Asset?> GetOwnedAssetAsync(
            Guid assetId,
            Guid ownerAccountId,
            bool tracking = true);

        Task<AssetSecret?> GetSecretAsync(
            Guid assetId,
            bool tracking = true);

        Task<MockExternalAccount?> FindMockExternalAccountAsync(
            string provider,
            string username);

        void Remove(Asset asset);

        Task SaveChangesAsync();
    }
}
