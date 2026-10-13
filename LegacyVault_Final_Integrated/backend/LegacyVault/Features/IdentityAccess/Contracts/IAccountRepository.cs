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
    }
}