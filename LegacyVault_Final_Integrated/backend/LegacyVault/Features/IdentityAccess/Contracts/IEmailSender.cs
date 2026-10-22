namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IEmailSender
    {
        Task SendOtpAsync(string toEmail, string otp);

        Task SendDmsReminderEmailAsync(
            string toEmail,
            string ownerName,
            DateTimeOffset dueDate);

        Task SendDmsTriggerEmailAsync(
            string toEmail,
            string executorName,
            string ownerName,
            string willTitle);
    }
}