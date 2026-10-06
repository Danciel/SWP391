using SWP301BETASK.Models;

namespace SWP301BETASK.Features.AssetManagement.Contracts
{
    public interface IAssetRepository
    {
        Task<bool> AccountExistsAsync(Guid accountId);

        Task<List<Asset>> GetAllByOwnerAsync(Guid ownerAccountId);

        Task<Asset?> GetByIdAsync(
            Guid assetId,
            Guid ownerAccountId);

        Task AddAsync(Asset asset);

        Task SaveChangesAsync();

        Task DeleteAsync(Asset asset);
    }
}