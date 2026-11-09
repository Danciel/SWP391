using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AssetManagement.Repositories
{
    public class AssetRepository : IAssetRepository
    {
        private readonly LegacyVaultDbContext _context;

        public AssetRepository(LegacyVaultDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Asset asset)
        {
            await _context.Assets.AddAsync(asset);
        }

        public async Task AddSecretAsync(AssetSecret secret)
        {
            await _context.AssetSecrets.AddAsync(secret);
        }

        public async Task AddVerificationAsync(
            AssetVerification verification)
        {
            await _context.AssetVerifications.AddAsync(verification);
        }

        public async Task<List<Asset>> GetOwnedAssetsAsync(
            Guid ownerAccountId)
        {
            return await _context.Assets
                .AsNoTracking()
                .Where(x => x.OwnerAccountId == ownerAccountId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<Asset?> GetOwnedAssetAsync(
            Guid assetId,
            Guid ownerAccountId,
            bool tracking = true)
        {
            IQueryable<Asset> query = _context.Assets;

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(x =>
                x.Id == assetId &&
                x.OwnerAccountId == ownerAccountId);
        }

        public async Task<AssetSecret?> GetSecretAsync(
            Guid assetId,
            bool tracking = true)
        {
            IQueryable<AssetSecret> query = _context.AssetSecrets;

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(
                x => x.AssetId == assetId);
        }

        public async Task<MockExternalAccount?>
            FindMockExternalAccountAsync(
                string provider,
                string username)
        {
            var normalizedProvider =
                provider.Trim().ToUpperInvariant();

            var normalizedUsername =
                username.Trim();

            return await _context.MockExternalAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Provider == normalizedProvider &&
                    x.Username == normalizedUsername &&
                    x.IsActive);
        }

        public void Remove(Asset asset)
        {
            _context.Assets.Remove(asset);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
