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

        public Task SendBeneficiaryInviteEmailAsync(
            string toEmail,
            string beneficiaryName,
            string ownerName)
        {
            var name = System.Net.WebUtility.HtmlEncode(beneficiaryName);
            var owner = System.Net.WebUtility.HtmlEncode(ownerName);
            var url = System.Net.WebUtility.HtmlEncode(
                _configuration["App:FrontendUrl"] ?? "http://localhost:5173");

            return SendAsync(
                toEmail,
                "LegacyVault - You have been named as a beneficiary",
                $"""
                <h2>Invitation</h2>

                <p>Hello {name},</p>

                <p>{owner} named you as a beneficiary in LegacyVault and a
                handover has started.</p>

                <p>Please register with <b>this email address</b> at
                <a href="{url}">{url}</a>, then verify your identity.</p>
                """);
        }

        public Task SendPinLockedEmailAsync(
            string toEmail,
            string fullName,
            DateTimeOffset lockedUntil)
        {
            var name = System.Net.WebUtility.HtmlEncode(fullName);

            return SendAsync(
                toEmail,
                "LegacyVault - Your Security PIN is locked",
                $"""
                <h2>Security PIN locked</h2>

                <p>Hello {name},</p>

                <p>A wrong Security PIN was entered 3 times in a row, so the PIN
                is locked until <b>{lockedUntil:yyyy-MM-dd HH:mm} UTC</b>.</p>

                <p>If this was you, wait until then and try again, or use
                "Forgot PIN" to reset it with a code sent to this email.</p>

                <p>If this was <b>not</b> you, someone may know your password:
                change it now and reset your PIN.</p>
                """);
        }

        public Task SendHandoverCodeEmailAsync(
            string toEmail,
            string recipientName,
            string ownerName,
            string code)
        {
            var name = System.Net.WebUtility.HtmlEncode(recipientName);
            var owner = System.Net.WebUtility.HtmlEncode(ownerName);
            var safeCode = System.Net.WebUtility.HtmlEncode(code);

            return SendAsync(
                toEmail,
                "LegacyVault - Your handover access code",
                $"""
                <h2>Your handover access code</h2>

                <p>Hello {name},</p>

                <p>{owner}'s Executor has handed assets over to you.
                To receive them, sign in to LegacyVault, open the inherited will
                and enter this code:</p>

                <h1 style="letter-spacing:2px;font-family:monospace">{safeCode}</h1>

                <p>The code only works with your own account. You will also be asked
                for your Security PIN and a face scan.</p>

                <p>3 wrong codes lock it for 15 minutes. Never share it with anyone;
                LegacyVault staff will never ask for it.</p>
                """);
        }

        public Task SendHandoverCodeLockedEmailAsync(
            string toEmail,
            string recipientName,
            string beneficiaryName,
            DateTimeOffset lockedUntil,
            bool toOwner)
        {
            var name = System.Net.WebUtility.HtmlEncode(recipientName);
            var who = System.Net.WebUtility.HtmlEncode(beneficiaryName);

            var message = toOwner
                ? $"<p>{who} entered a wrong handover access code 3 times, so it is locked until <b>{lockedUntil:yyyy-MM-dd HH:mm} UTC</b>. If you do not expect this, tell your Executor.</p>"
                : $"<p>A wrong handover access code was entered 3 times, so it is locked until <b>{lockedUntil:yyyy-MM-dd HH:mm} UTC</b>. After that you can try again, or ask for a new code on the inherited will page.</p>";

            return SendAsync(
                toEmail,
                "LegacyVault - Handover access code locked",
                $"""
                <h2>Access code locked</h2>

                <p>Hello {name},</p>

                {message}
                """);
        }

        public Task SendHandoverSupportRequestEmailAsync(
            string toEmail,
            string toName,
            string recipientName,
            string willTitle)
        {
            var name = System.Net.WebUtility.HtmlEncode(toName);
            var who = System.Net.WebUtility.HtmlEncode(recipientName);
            var title = System.Net.WebUtility.HtmlEncode(willTitle);

            return SendAsync(
                toEmail,
                "LegacyVault - A recipient needs help with an access code",
                $"""
                <h2>A recipient needs help</h2>

                <p>Hello {name},</p>

                <p><b>{who}</b> cannot enter the handover access code for
                <b>{title}</b> (the limit of new codes was reached, or the code is locked).</p>

                <p>Open the Handovers card in LegacyVault and press <b>Send a new code</b>
                next to this recipient, or contact the support team.</p>
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