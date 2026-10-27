using System.Security.Cryptography;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class EmailOtpService : IEmailOtpService
    {
        private readonly IEmailOtpRepository _emailOtpRepository;

        public EmailOtpService(IEmailOtpRepository emailOtpRepository)
        {
            _emailOtpRepository = emailOtpRepository;
        }

        public async Task<string> GenerateAndSaveOtpAsync(Guid accountId)
        {
            // Sinh số ngẫu nhiên từ 000000 đến 999999
            int otpNumber = RandomNumberGenerator.GetInt32(0, 1_000_000);

            // Luôn đảm bảo đủ 6 chữ số
            string otp = otpNumber.ToString("D6");

            // Không lưu OTP thật vào database
            string otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

            var emailOtp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                OtpHash = otpHash,

                // OTP có hiệu lực trong 5 phút
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),

                UsedAt = null,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _emailOtpRepository.AddAsync(emailOtp);
            await _emailOtpRepository.SaveChangesAsync();

            // Trả OTP thật để bước sau gửi qua Gmail
            return otp;
        }
    }
}