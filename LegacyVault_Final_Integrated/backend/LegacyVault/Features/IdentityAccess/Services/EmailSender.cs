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

        public Task SendOtpAsync(string toEmail, string otp)
        {
            return SendAsync(
                toEmail,
                "LegacyVault - Email Verification OTP",
                $"""
                <h2>LegacyVault Email Verification</h2>

                <p>Your verification code is:</p>

                <h1>{otp}</h1>

                <p>This code will expire in 5 minutes.</p>

                <p>If you did not request this code, please ignore this email.</p>
                """);
        }

        public Task SendDmsReminderEmailAsync(
            string toEmail,
            string ownerName,
            DateTimeOffset dueDate)
        {
            var name = System.Net.WebUtility.HtmlEncode(ownerName);

            return SendAsync(
                toEmail,
                "LegacyVault - Please confirm you are still active",
                $"""
                <h2>Check-in reminder</h2>

                <p>Hello {name},</p>

                <p>Your next LegacyVault check-in is due on
                <b>{dueDate:yyyy-MM-dd HH:mm} UTC</b>.</p>

                <p>Please sign in and press "I'm Still Active".</p>
                """);
        }

        public Task SendDmsTriggerEmailAsync(
            string toEmail,
            string executorName,
            string ownerName,
            string willTitle)
        {
            var exec = System.Net.WebUtility.HtmlEncode(executorName);
            var owner = System.Net.WebUtility.HtmlEncode(ownerName);
            var title = System.Net.WebUtility.HtmlEncode(willTitle);

            return SendAsync(
                toEmail,
                "LegacyVault - Owner missed check-ins",
                $"""
                <h2>Action may be required</h2>

                <p>Hello {exec},</p>

                <p>{owner} has missed the allowed number of check-ins
                for the will "{title}".</p>

                <p>This is NOT a confirmation of death and no handover has
                started. Please sign in to your Executor inbox.</p>
                """);
        }

        private async Task SendAsync(
            string toEmail,
            string subject,
            string html)
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
                new MailboxAddress("LegacyVault", senderEmail));

            message.To.Add(MailboxAddress.Parse(toEmail));

            message.Subject = subject;

            message.Body = new TextPart("html") { Text = html };

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