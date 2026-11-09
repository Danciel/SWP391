using LegacyVault.Features.IdentityAccess.Contracts;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class MockSmsSender : ISmsSender
    {
        private readonly ILogger<MockSmsSender> _logger;

        public MockSmsSender(ILogger<MockSmsSender> logger) => _logger = logger;

        public Task SendOtpAsync(string phoneNumber, string otp)
        {
            // This will print in your backend terminal!
            _logger.LogWarning("\n==============================================");
            _logger.LogWarning($"[MOCK SMS] To: {phoneNumber}");
            _logger.LogWarning($"[MOCK SMS] Message: Your LegacyVault Phone OTP is {otp}");
            _logger.LogWarning("==============================================\n");

            return Task.CompletedTask;
        }
    }
}