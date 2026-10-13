namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IEmailSender
    {
        Task SendOtpAsync(string toEmail, string otp);
    }
}