using System.Text.Json;
using LegacyVault.Features.IdentityAccess.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class TextbeltSmsSender : ISmsSender
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TextbeltSmsSender> _logger;
        private readonly IConfiguration _config;

        public TextbeltSmsSender(HttpClient httpClient, ILogger<TextbeltSmsSender> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public async Task SendOtpAsync(string phoneNumber, string otp)
        {
            var apiKey = _config["TextbeltSettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Textbelt API Key is missing from configuration.");
                throw new InvalidOperationException("SMS configuration is invalid.");
            }
            // 1. Ensure the phone number is in E.164 format (e.g., +84912345678)
            if (!phoneNumber.StartsWith("+"))
            {
                if (phoneNumber.StartsWith("0"))
                    phoneNumber = "+84" + phoneNumber.Substring(1);
                else
                    phoneNumber = "+84" + phoneNumber;
            }

            // 2. Compose the message. Vietnam regulations require a brand name.
            var messageBody = $"[LegacyVault] Your verification code is: {otp}. Valid for 5 minutes. Do not share this code.";

            // 3. Prepare the request payload for Textbelt
            var requestData = new Dictionary<string, string>
            {
                { "phone", phoneNumber },
                { "message", messageBody },
                { "key", apiKey } // The free, shared key for testing
            };

            try
            {
                var response = await _httpClient.PostAsync("https://textbelt.com/text", new FormUrlEncodedContent(requestData));
                var responseString = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;

                // Safely check if "success" exists and is true
                var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                if (success)
                {
                    // Safely extract textId (handles both Number and String types)
                    var textId = root.TryGetProperty("textId", out var textIdProp)
                        ? textIdProp.ToString()
                        : "unknown";

                    _logger.LogInformation($"SMS sent successfully to {phoneNumber} via Textbelt. TextId: {textId}");
                }
                else
                {
                    // Safely extract the error message
                    var error = root.TryGetProperty("error", out var errorProp)
                        ? errorProp.GetString()
                        : "Unknown error from Textbelt";

                    _logger.LogError($"Failed to send SMS via Textbelt. Error: {error}");
                    throw new InvalidOperationException($"Failed to send SMS: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred while sending SMS to {phoneNumber}");
                throw new InvalidOperationException("Failed to send SMS due to a network or system error.", ex);
            }
        }
    }
}