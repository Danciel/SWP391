namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IEmailOtpService
    {
        Task<string> GenerateAndSaveOtpAsync(Guid accountId);
    }
}