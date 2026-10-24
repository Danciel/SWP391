using LegacyVault.Features.IdentityAccess.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class TwilioSmsSender : ISmsSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TwilioSmsSender> _logger;

        public TwilioSmsSender(IConfiguration config, ILogger<TwilioSmsSender> logger)
        {
            _config = config;
            _logger = logger;

            // Initialize Twilio Client once
            var accountSid = _config["SmsSettings:AccountSid"];
            var authToken = _config["SmsSettings:AuthToken"];

            if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
            {
                _logger.LogWarning("Twilio credentials are missing. SMS will not be sent.");
                return;
            }

            TwilioClient.Init(accountSid, authToken);
        }

        public async Task SendOtpAsync(string phoneNumber, string otp)
        {
            var messagingServiceSid = _config["SmsSettings:MessagingServiceSid"];

            // Ensure the phone number is in E.164 format (e.g., +84987654321)
            // If your users enter "0987654321", Twilio will reject it.
            if (!phoneNumber.StartsWith("+"))
            {
                // Assuming Vietnam country code (+84) as default.
                // You might need to strip the leading "0" first.
                if (phoneNumber.StartsWith("0"))
                {
                    phoneNumber = "+84" + phoneNumber.Substring(1);
                }
                else
                {
                    phoneNumber = "+84" + phoneNumber;
                }
            }

            // CRITICAL FOR VIETNAM: Brand name must be at the beginning of the body.
                        // The brand name should match your registered Alpha Sender ID.
            var messageBody = $"[LegacyVault] Your verification code is {otp}. Valid for 5 minutes. Do not share this code.";


            try
            {
                var message = await MessageResource.CreateAsync(
                    body: messageBody,
                    messagingServiceSid: messagingServiceSid,
                    to: new PhoneNumber(phoneNumber)
                );

                _logger.LogInformation($"SMS sent successfully to {phoneNumber}. SID: {message.Sid}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
                throw new InvalidOperationException("Failed to send SMS. Please check the phone number and try again.");
            }
        }
    }
}