using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IEmailOtpRepository
    {
        Task AddAsync(EmailOtp emailOtp);

        Task<EmailOtp?> GetLatestUnusedAsync(Guid accountId);

        Task SaveChangesAsync();
    }
}