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
        public async Task<(IReadOnlyList<Account> Items, int Total)> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? role,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Accounts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(a => a.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                query = query.Where(a =>
                    a.FullName.ToLower().Contains(term) ||
                    a.Email.ToLower().Contains(term) ||
                    (a.Phone != null && a.Phone.Contains(term)));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .ThenBy(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLower();

            return _context.Accounts.AnyAsync(
                a => a.Email.ToLower() == normalized &&
                     (excludeId == null || a.Id != excludeId),
                cancellationToken);
        }

        public Task<bool> PhoneExistsAsync(
            string phone,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = phone.Trim();

            return _context.Accounts.AnyAsync(
                a => a.Phone == normalized &&
                     (excludeId == null || a.Id != excludeId),
                cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        {
            // GetByIdAsync returns a tracked entity, so SaveChanges is enough;
            // Update() also covers the case of a detached entity.
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync(cancellationToken);
        }        
    }
}