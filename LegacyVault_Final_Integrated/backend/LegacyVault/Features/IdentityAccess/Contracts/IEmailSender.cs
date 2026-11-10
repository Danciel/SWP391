namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IEmailSender
    {
        Task SendOtpAsync(string toEmail, string otp);

        Task SendDmsReminderEmailAsync(
            string toEmail,
            string ownerName,
            DateTimeOffset dueDate);

        Task SendBeneficiaryInviteEmailAsync(
            string toEmail,
            string beneficiaryName,
            string ownerName);

        Task SendDmsTriggerEmailAsync(
            string toEmail,
            string executorName,
            string ownerName,
            string willTitle);

        // The Security PIN was locked after too many wrong attempts.
        Task SendPinLockedEmailAsync(
            string toEmail,
            string fullName,
            DateTimeOffset lockedUntil);

        // The recipient's handover access code (XXXX-XXXX-XXXX).
        Task SendHandoverCodeEmailAsync(
            string toEmail,
            string recipientName,
            string ownerName,
            string code);

        // The access code was locked after 3 wrong codes.
        // toOwner = true: the owner is told that someone failed to enter it.
        Task SendHandoverCodeLockedEmailAsync(
            string toEmail,
            string recipientName,
            string beneficiaryName,
            DateTimeOffset lockedUntil,
            bool toOwner);

        // The recipient used all their new codes (or is locked) and asks for help.
        Task SendHandoverSupportRequestEmailAsync(
            string toEmail,
            string toName,
            string recipientName,
            string willTitle);
    }
}