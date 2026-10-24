using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.IdentityAccess.Repositories
{
    public class PhoneOtpRepository : IPhoneOtpRepository
    {
        private readonly LegacyVaultDbContext _db; // use your DbContext name
        public PhoneOtpRepository(LegacyVaultDbContext db) => _db = db;

        public async Task AddAsync(PhoneOtp otp) => await _db.PhoneOtps.AddAsync(otp);

        public Task<PhoneOtp?> GetLatestAsync(Guid accountId) =>
            _db.PhoneOtps
               .Where(x => x.AccountId == accountId)
               .OrderByDescending(x => x.CreatedAt)
               .FirstOrDefaultAsync();

        public Task<PhoneOtp?> GetLatestUnusedAsync(Guid accountId) =>
            _db.PhoneOtps
               .Where(x => x.AccountId == accountId && x.UsedAt == null)
               .OrderByDescending(x => x.CreatedAt)
               .FirstOrDefaultAsync();

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}