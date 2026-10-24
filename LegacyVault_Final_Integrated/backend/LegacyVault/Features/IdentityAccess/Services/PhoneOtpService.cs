using System.Security.Cryptography;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class PhoneOtpService : IPhoneOtpService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

        private readonly IPhoneOtpRepository _repo;

        public PhoneOtpService(IPhoneOtpRepository repo) => _repo = repo;

        public async Task<string> GenerateAndSaveOtpAsync(Guid accountId)
        {
            var latest = await _repo.GetLatestAsync(accountId);
            if (latest is not null &&
                latest.CreatedAt > DateTimeOffset.UtcNow - ResendCooldown)
            {
                throw new InvalidOperationException(
                    "Please wait before requesting another code.");
            }

            string otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            await _repo.AddAsync(new PhoneOtp
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                OtpHash = BCrypt.Net.BCrypt.HashPassword(otp),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _repo.SaveChangesAsync();

            return otp;
        }

        public async Task<bool> VerifyAsync(Guid accountId, string otp)
        {
            var entry = await _repo.GetLatestAsync(accountId);

            if (entry is null ||
                entry.UsedAt is not null ||
                entry.ExpiresAt < DateTimeOffset.UtcNow ||
                entry.FailedAttempts >= MaxFailedAttempts)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(otp, entry.OtpHash))
            {
                entry.FailedAttempts++;
                await _repo.SaveChangesAsync();
                return false;
            }

            entry.UsedAt = DateTimeOffset.UtcNow; // single use
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}