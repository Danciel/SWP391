using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IPhoneOtpService
    {
        Task<string> GenerateAndSaveOtpAsync(Guid accountId);
        Task<bool> VerifyAsync(Guid accountId, string otp);
    }
}