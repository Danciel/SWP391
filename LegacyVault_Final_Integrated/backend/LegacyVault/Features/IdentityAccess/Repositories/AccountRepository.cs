using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.IdentityAccess.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly LegacyVaultDbContext _context;

        public AccountRepository(LegacyVaultDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _context.Accounts
                .AnyAsync(x => x.Email.ToLower() == normalizedEmail);
        }

        public async Task<bool> PhoneExistsAsync(string phoneNumber)
        {
            var normalizedPhone = phoneNumber.Trim();

            return await _context.Accounts
                .AnyAsync(x => x.Phone == normalizedPhone);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _context.Accounts
                .FirstOrDefaultAsync(
                    x => x.Email.ToLower() == normalizedEmail
                );
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<Account?> SearchByEmailAsync(string email)
        {
            var normalizedEmail =
                email.Trim().ToLower();

            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Email.ToLower() == normalizedEmail);
        }

        public async Task<Account?> SearchByPhoneAsync(string phoneNumber)
        {
            var normalizedPhone =
                phoneNumber.Trim();

            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Phone == normalizedPhone);
        }
    }
}