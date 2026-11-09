using MockIdpApi.Data;
using MockIdpApi.Features.IdentityAccess.Contracts;
using MockIdpApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MockIdpApi.Features.IdentityAccess.Repositories
{
    public class IdpUserRepository : IIdpUserRepository
    {
        private readonly MockIdpDbContext _context;

        public IdpUserRepository(MockIdpDbContext context)
        {
            _context = context;
        }

        public async Task<IdpUser?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.IdpUsers
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<IdpUser?> SearchByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return await _context.IdpUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Email.ToLower() == normalizedEmail,
                    cancellationToken);
        }

        public async Task<IdpUser?> SearchByPhoneAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            var normalizedPhone = phoneNumber.Trim();

            return await _context.IdpUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Phone == normalizedPhone,
                    cancellationToken);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();

            return _context.IdpUsers.AnyAsync(
                x => x.Email.ToLower() == normalized &&
                     (excludeId == null || x.Id != excludeId),
                cancellationToken);
        }

        public Task<bool> PhoneExistsAsync(
            string phoneNumber,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var normalized = phoneNumber.Trim();

            return _context.IdpUsers.AnyAsync(
                x => x.Phone == normalized &&
                     (excludeId == null || x.Id != excludeId),
                cancellationToken);
        }

        public async Task<(IReadOnlyList<IdpUser> Items, int Total)> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? identityStatus,
            CancellationToken cancellationToken = default)
        {
            var query = _context.IdpUsers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(identityStatus))
            {
                query = query.Where(x => x.IdentityStatus == identityStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();

                query = query.Where(x =>
                    x.FullName.ToLower().Contains(term) ||
                    x.Email.ToLower().Contains(term) ||
                    (x.Phone != null && x.Phone.Contains(term)));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task AddAsync(
            IdpUser user,
            CancellationToken cancellationToken = default)
        {
            await _context.IdpUsers.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(
            IdpUser user,
            CancellationToken cancellationToken = default)
        {
            _context.IdpUsers.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(
            IdpUser user,
            CancellationToken cancellationToken = default)
        {
            _context.IdpUsers.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}