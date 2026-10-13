using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.IdentityAccess.Repositories
{
    public class EmailOtpRepository : IEmailOtpRepository
    {
        private readonly LegacyVaultDbContext _context;

        public EmailOtpRepository(LegacyVaultDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EmailOtp emailOtp)
        {
            await _context.EmailOtps.AddAsync(emailOtp);
        }

        public async Task<EmailOtp?> GetLatestUnusedAsync(Guid accountId)
        {
            return await _context.EmailOtps
                .Where(x =>
                    x.AccountId == accountId &&
                    x.UsedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}