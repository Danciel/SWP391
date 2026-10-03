using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.AssetManagement.Contracts;
using SWP301BETASK.Models;

namespace SWP301BETASK.Features.AssetManagement.Repositories
{
    public class AssetRepository : IAssetRepository
    {
        private readonly LegacyVaultDbContext _db;

        public AssetRepository(LegacyVaultDbContext db)
        {
            _db = db;
        }

        public async Task<bool> AccountExistsAsync(Guid accountId)
        {
            return await _db.Accounts
                .AnyAsync(a => a.Id == accountId);
        }

        public async Task<List<Asset>> GetAllByOwnerAsync(Guid ownerAccountId)
        {
            return await _db.Assets
                .AsNoTracking()
                .Include(a => a.Secret)
                .Where(a => a.OwnerAccountId == ownerAccountId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Asset?> GetByIdAsync(
            Guid assetId,
            Guid ownerAccountId)
        {
            return await _db.Assets
                .Include(a => a.Secret)
                .Include(a => a.Verifications)
                .FirstOrDefaultAsync(a =>
                    a.Id == assetId &&
                    a.OwnerAccountId == ownerAccountId);
        }

        public async Task AddAsync(Asset asset)
        {
            await _db.Assets.AddAsync(asset);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Asset asset)
        {
            _db.Assets.Remove(asset);
            await _db.SaveChangesAsync();
        }
    }
}