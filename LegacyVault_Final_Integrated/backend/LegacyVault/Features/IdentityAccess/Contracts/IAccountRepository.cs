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

        Task SaveChangesAsync();
        Task<Account?> SearchByEmailAsync(string email);
        Task<Account?> SearchByPhoneAsync(string phoneNumber);

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