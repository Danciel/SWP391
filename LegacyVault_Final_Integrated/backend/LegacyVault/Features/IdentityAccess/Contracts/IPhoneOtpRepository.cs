using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IPhoneOtpRepository
    {
        Task AddAsync(PhoneOtp otp);
        Task<PhoneOtp?> GetLatestAsync(Guid accountId);
        Task SaveChangesAsync();
        Task<PhoneOtp?> GetLatestUnusedAsync(Guid accountId);
    }
}
