using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface ISmsSender
    {
        Task SendOtpAsync(string toPhone, string otp);
    }
}