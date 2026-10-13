using LegacyVault.Features.IdentityAccess.Contracts;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(
                _configuration["Email:SmtpPort"] ?? "587");

            var senderEmail = _configuration["Email:SenderEmail"];
            var appPassword = _configuration["Email:AppPassword"];

            if (string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(appPassword))
            {
                throw new InvalidOperationException(
                    "Email configuration is missing.");
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "LegacyVault",
                    senderEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject = "LegacyVault - Email Verification OTP";

            message.Body = new TextPart("html")
            {
                Text = $"""
                    <h2>LegacyVault Email Verification</h2>

                    <p>Your verification code is:</p>

                    <h1>{otp}</h1>

                    <p>This code will expire in 5 minutes.</p>

                    <p>If you did not request this code, please ignore this email.</p>
                    """
            };

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                smtpHost,
                smtpPort,
                SecureSocketOptions.StartTls);

            await smtpClient.AuthenticateAsync(
                senderEmail,
                appPassword);

            await smtpClient.SendAsync(message);

            await smtpClient.DisconnectAsync(true);
        }
    }
}