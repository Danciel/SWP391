using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IAccountRepository
    {
        Task<bool> EmailExistsAsync(string email);

        Task<bool> PhoneExistsAsync(string phoneNumber);

        Task<Account?> GetByEmailAsync(string email);

        Task<Account?> GetByIdAsync(Guid id);

        Task AddAsync(Account account);

        // ---- Uniqueness checks ----
        // excludeId lets an update ignore the row being edited;
        // CancellationToken lets callers abort long queries.

        Task SaveChangesAsync();
        Task<Account?> SearchByEmailAsync(string email);
        Task<Account?> SearchByPhoneAsync(string phoneNumber);

        Task<bool> EmailExistsAsync(
            string email,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);

        Task<bool> PhoneExistsAsync(
            string phone,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);

        // ---- New: admin CRUD ----
        Task<(IReadOnlyList<Account> Items, int Total)> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? role,
            CancellationToken cancellationToken = default);

        Task AddAsync(Account account, CancellationToken cancellationToken = default);

        Task UpdateAsync(Account account, CancellationToken cancellationToken = default);

        Task DeleteAsync(Account account, CancellationToken cancellationToken = default);
    }
}