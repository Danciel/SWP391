using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Repositories
{
    public class WillRepository : IWillRepository
    {
        private readonly LegacyVaultDbContext _context;

        public WillRepository(LegacyVaultDbContext context)
        {
            _context = context;
        }

        public async Task AddWillAsync(Will will)
        {
            await _context.Wills.AddAsync(will);
        }

        public async Task<List<Will>> GetOwnedWillsAsync(
            Guid ownerAccountId)
        {
            return await _context.Wills
                .AsNoTracking()
                .Where(x => x.OwnerAccountId == ownerAccountId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<Will?> GetOwnedWillAsync(
            Guid willId,
            Guid ownerAccountId,
            bool includeDetails = false)
        {
            IQueryable<Will> query = _context.Wills;

            if (includeDetails)
            {
                query = query
                    .Include(x => x.WillAssets)
                        .ThenInclude(x => x.Asset)
                    .Include(x => x.WillAssets)
                        .ThenInclude(x => x.Assignments)
                    .Include(x => x.Beneficiaries)
                    .Include(x => x.ExecutorAssignment)
                        .ThenInclude(x => x.ExecutorAccount)
                    .Include(x => x.DmsConfiguration);
            }

            return await query.FirstOrDefaultAsync(x =>
                x.Id == willId &&
                x.OwnerAccountId == ownerAccountId);
        }

        public async Task<Asset?> GetOwnedAssetAsync(
            Guid assetId,
            Guid ownerAccountId)
        {
            return await _context.Assets
                .FirstOrDefaultAsync(x =>
                    x.Id == assetId &&
                    x.OwnerAccountId == ownerAccountId);
        }

        public async Task<Account?> GetAccountAsync(
            Guid accountId)
        {
            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == accountId);
        }

        public async Task<ExecutorProfile?>
            GetExecutorProfileAsync(Guid accountId)
        {
            return await _context.ExecutorProfiles
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x =>
                    x.AccountId == accountId);
        }

        public async Task<List<ExecutorProfile>>
            GetVerifiedExecutorsAsync()
        {
            return await _context.ExecutorProfiles
                .AsNoTracking()
                .Include(x => x.Account)
                .Where(x =>
                    x.IsVerified &&
                    x.Account != null &&
                    x.Account.Role == "EXECUTOR")
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.CompletedCases)
                .ToListAsync();
        }

        public async Task<WillAsset?> GetWillAssetAsync(
            Guid willAssetId)
        {
            return await _context.WillAssets
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x =>
                    x.Id == willAssetId);
        }

        public async Task<BeneficiaryDesignation?>
            GetBeneficiaryAsync(Guid beneficiaryId)
        {
            return await _context.BeneficiaryDesignations
                .FirstOrDefaultAsync(x =>
                    x.Id == beneficiaryId);
        }

        public async Task<bool> AssignmentExistsAsync(
            Guid willAssetId,
            Guid beneficiaryDesignationId)
        {
            return await _context.AssetAssignments
                .AnyAsync(x =>
                    x.WillAssetId == willAssetId &&
                    x.BeneficiaryDesignationId ==
                        beneficiaryDesignationId);
        }

        public async Task<ExecutorAssignment?>
            GetExecutorAssignmentAsync(Guid willId)
        {
            return await _context.ExecutorAssignments
                .Include(x => x.ExecutorAccount)
                .FirstOrDefaultAsync(x =>
                    x.WillId == willId);
        }

        public async Task<DmsConfiguration?>
            GetDmsConfigurationAsync(Guid willId)
        {
            return await _context.DmsConfigurations
                .FirstOrDefaultAsync(x =>
                    x.WillId == willId);
        }

        public async Task AddWillAssetAsync(
            WillAsset willAsset)
        {
            await _context.WillAssets.AddAsync(willAsset);
        }

        public async Task AddBeneficiaryAsync(
            BeneficiaryDesignation beneficiary)
        {
            await _context.BeneficiaryDesignations
                .AddAsync(beneficiary);
        }

        public async Task AddAssetAssignmentAsync(
            AssetAssignment assignment)
        {
            await _context.AssetAssignments
                .AddAsync(assignment);
        }

        public async Task AddExecutorAssignmentAsync(
            ExecutorAssignment assignment)
        {
            await _context.ExecutorAssignments
                .AddAsync(assignment);
        }

        public async Task AddDmsConfigurationAsync(
            DmsConfiguration configuration)
        {
            await _context.DmsConfigurations
                .AddAsync(configuration);
        }

        public async Task AddDmsCheckInAsync(
            DmsCheckIn checkIn)
        {
            await _context.DmsCheckIns.AddAsync(checkIn);
        }

        public void RemoveWill(Will will)
        {
            _context.Wills.Remove(will);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
