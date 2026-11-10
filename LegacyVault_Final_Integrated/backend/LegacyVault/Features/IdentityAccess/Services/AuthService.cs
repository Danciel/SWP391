using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using LegacyVault.Features.IdentityAccess.Repositories;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using LegacyVault.Data;
using Microsoft.Extensions.Caching.Memory;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailOtpService _emailOtpService;
        private readonly IEmailSender _emailSender;
        private readonly IEmailOtpRepository _emailOtpRepository;
        private readonly IPhoneOtpService _phoneOtpService;
        private readonly ISmsSender _smsSender;
        private readonly IPhoneOtpRepository _phoneOtpRepository;
        private readonly LegacyVaultDbContext _db;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMemoryCache _cache;
        private readonly IBeneficiaryAutoLinkService _beneficiaryAutoLink;

        // Email OTP: at most 5 tries per code, and one new code per minute.
        private const int MaxOtpAttempts = 5;
        private static readonly object OtpAttemptLock = new();

        public AuthService(
    IAccountRepository accountRepository,
    IEmailOtpService emailOtpService,
    IEmailSender emailSender,
    IEmailOtpRepository emailOtpRepository,
    IJwtTokenService jwtTokenService,
    IPhoneOtpService phoneOtpService,
    ISmsSender smsSender,
    IPhoneOtpRepository phoneOtpRepository,
    LegacyVaultDbContext db)
    IPhoneOtpRepository phoneOtpRepository,
    IMemoryCache cache,
    IBeneficiaryAutoLinkService beneficiaryAutoLink)
        {
            _accountRepository = accountRepository;
            _emailOtpService = emailOtpService;
            _emailSender = emailSender;
            _emailOtpRepository = emailOtpRepository;
            _jwtTokenService = jwtTokenService;
            _phoneOtpService = phoneOtpService;
            _smsSender = smsSender;
            _phoneOtpRepository = phoneOtpRepository;
            _db = db;
            _cache = cache;
            _beneficiaryAutoLink = beneficiaryAutoLink;
        }

        // A verified account whose email and name match a beneficiary designation
        // joins that handover without the owner. Never blocks sign-in / verification.
        private async Task TryAutoLinkBeneficiariesAsync(Guid accountId)
        {
            try
            {
                await _beneficiaryAutoLink.TryLinkAccountAsync(accountId);
            }
            catch (Exception)
            {
                // Linking is best effort: the owner can still link manually.
            }
        }

        public async Task<AccountResponse> RegisterAsync(
            RegisterRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            // 1. Kiểm tra email đã tồn tại chưa
            if (await _accountRepository.EmailExistsAsync(email))
            {
                throw new InvalidOperationException(
                    "Email is already registered."
                );
            }

            // 2. Nếu có phone thì kiểm tra trùng
            if (phoneNumber != null &&
                await _accountRepository.PhoneExistsAsync(phoneNumber))
            {
                throw new InvalidOperationException(
                    "Phone number is already registered."
                );
            }

            // 3. Hash password
            var passwordHash =
                BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Tạo Account
            var account = new Account
            {
                Id = Guid.NewGuid(),

                FullName = request.FullName.Trim(),

                Email = email,

                Phone = phoneNumber,

                PasswordHash = passwordHash,

                // Business Rule:
                // User đăng ký luôn là USER
                Role = "USER",

                EmailVerified = false,

                PhoneVerified = false,

                IdentityStatus = "UNVERIFIED",

                ExternalIdentityId = null,

                CreatedAt = DateTimeOffset.UtcNow,

                UpdatedAt = DateTimeOffset.UtcNow
            };

            // 5. Lưu xuống Supabase
            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveChangesAsync();

            var otp =
                await _emailOtpService.GenerateAndSaveOtpAsync(account.Id);

            await _emailSender.SendOtpAsync(
                account.Email,
                otp);

            // 6. Trả DTO an toàn
            return new AccountResponse
            {
                Id = account.Id,

                FullName = account.FullName,

                Email = account.Email,

                PhoneNumber = account.Phone,

                Role = account.Role,

                EmailVerified = account.EmailVerified,

                PhoneVerified = account.PhoneVerified,

                IdentityStatus = account.IdentityStatus,

                CreatedAt = account.CreatedAt
            };
        }

        public async Task VerifyEmailOtpAsync(VerifyEmailOtpRequest request)
        {
            var email = request.Email.Trim().ToLower();

            // 1. Tìm account theo email
            var account = await _accountRepository.GetByEmailAsync(email);

            if (account == null)
            {
                throw new InvalidOperationException(
                    "Account not found.");
            }

            // 2. Nếu email đã verify rồi
            if (account.EmailVerified)
            {
                throw new InvalidOperationException(
                    "Email is already verified.");
            }

            // 3. Lấy OTP mới nhất chưa được sử dụng
            var emailOtp =
                await _emailOtpRepository.GetLatestUnusedAsync(account.Id);

            if (emailOtp == null)
            {
                throw new InvalidOperationException(
                    "OTP not found.");
            }

            // 4. Kiểm tra OTP đã hết hạn chưa
            if (emailOtp.ExpiresAt < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "OTP has expired.");
            }

            // 5. Giới hạn số lần thử: đếm TRƯỚC khi so sánh để các request song song
            //    không vượt quá giới hạn.
            var attemptsKey = $"email-otp-attempts:{account.Id}";
            int attempts;

            lock (OtpAttemptLock)
            {
                _cache.TryGetValue(attemptsKey, out attempts);
                attempts++;
                _cache.Set(attemptsKey, attempts, TimeSpan.FromMinutes(15));
            }

            if (attempts > MaxOtpAttempts)
            {
                throw new InvalidOperationException(
                    "Too many invalid attempts. Please request a new code.");
            }

            // 6. So sánh OTP user nhập với hash trong database
            var isValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Otp,
                    emailOtp.OtpHash);

            if (!isValid)
            {
                if (attempts >= MaxOtpAttempts)
                {
                    // Hết lượt: mã này không dùng được nữa.
                    emailOtp.UsedAt = DateTimeOffset.UtcNow;
                    await _accountRepository.SaveChangesAsync();

                    throw new InvalidOperationException(
                        "Too many invalid attempts. Please request a new code.");
                }

                throw new InvalidOperationException(
                    "Invalid OTP.");
            }

            _cache.Remove(attemptsKey);

            // 7. OTP đúng → xác nhận email
            account.EmailVerified = true;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            // 8. Đánh dấu OTP đã được sử dụng
            emailOtp.UsedAt = DateTimeOffset.UtcNow;

            // Account và EmailOtp đang được cùng DbContext theo dõi
            await _accountRepository.SaveChangesAsync();

            // 9. Email đã xác minh: gắn vào các bàn giao đang chạy (nếu khớp)
            await TryAutoLinkBeneficiariesAsync(account.Id);
        }

        public async Task ResendEmailOtpAsync(
    ResendEmailOtpRequest request)
        {
            var email = request.Email.Trim().ToLower();

            // 1. Tìm account
            var account =
                await _accountRepository.GetByEmailAsync(email);

            if (account == null)
            {
                throw new InvalidOperationException(
                    "Account not found.");
            }

            // 2. Nếu email đã verify rồi thì không cần resend
            if (account.EmailVerified)
            {
                throw new InvalidOperationException(
                    "Email is already verified.");
            }

            // 3. Chờ 1 phút giữa hai lần gửi để không bị spam email
            var cooldownKey = $"email-otp-resend:{account.Id}";

            if (_cache.TryGetValue(cooldownKey, out _))
            {
                throw new InvalidOperationException(
                    "Please wait a minute before requesting another code.");
            }

            _cache.Set(cooldownKey, true, TimeSpan.FromMinutes(1));

            // Mã mới => đếm lại số lần thử
            _cache.Remove($"email-otp-attempts:{account.Id}");

            // 4. Sinh OTP mới và lưu hash xuống database
            var otp =
                await _emailOtpService
                    .GenerateAndSaveOtpAsync(account.Id);

            // 5. Gửi OTP mới qua Gmail
            await _emailSender.SendOtpAsync(
                account.Email,
                otp);
        }
        public async Task<LoginResponse> LoginAsync(
    LoginRequest request)
        {
            var email = request.Email.Trim().ToLower();

            // 1. Tìm account theo email
            var account =
                await _accountRepository.GetByEmailAsync(email);

            if (account == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            // 2. Kiểm tra password
            var passwordCorrect =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    account.PasswordHash);

            if (!passwordCorrect)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            // 3. Email phải được verify trước
            if (!account.EmailVerified)
            {
                throw new InvalidOperationException(
                    "Email is not verified.");
            }

            // Đã xác minh email: gắn vào bàn giao đang chạy nếu có (best effort)
            await TryAutoLinkBeneficiariesAsync(account.Id);

            if (account.IdentityStatus == "LOCKED")
                throw new UnauthorizedAccessException("This account is locked. Please contact support.");

            // 4. Tạo JWT
            var token =
                _jwtTokenService.GenerateToken(
                    account,
                    request.RememberMe);

            var refreshToken = await GenerateRefreshTokenAsync(account.Id);

            // 5. Trả token + thông tin user
            return new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,

                User = new AccountResponse
                {
                    Id = account.Id,
                    FullName = account.FullName,
                    Email = account.Email,
                    PhoneNumber = account.Phone,
                    Role = account.Role,
                    EmailVerified = account.EmailVerified,
                    PhoneVerified = account.PhoneVerified,
                    IdentityStatus = account.IdentityStatus,
                    CreatedAt = account.CreatedAt
                }
            };
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid accountId)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // Long-lived
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<LoginResponse> RefreshTokenAsync(string token)
        {
            var existing = await _db.RefreshTokens
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Token == token);

            if (existing == null || existing.IsRevoked || existing.ExpiresAt < DateTimeOffset.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            // TOKEN ROTATION: Revoke the old one, issue a new one
            existing.IsRevoked = true;
            existing.RevokedAt = DateTimeOffset.UtcNow;

            var newRefreshToken = await GenerateRefreshTokenAsync(existing.AccountId);
            var newJwt = _jwtTokenService.GenerateToken(existing.Account, false);

            return new LoginResponse
            {
                Token = newJwt,
                RefreshToken = newRefreshToken.Token,
                User = new AccountResponse
                {
                    Id = existing.Account.Id,
                    FullName = existing.Account.FullName,
                    Email = existing.Account.Email,
                    PhoneNumber = existing.Account.Phone,
                    Role = existing.Account.Role,
                    EmailVerified = existing.Account.EmailVerified,
                    PhoneVerified = existing.Account.PhoneVerified,
                    IdentityStatus = existing.Account.IdentityStatus,
                    CreatedAt = existing.Account.CreatedAt
                }
            };
        }

        public async Task RevokeTokenAsync(string token)
        {
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
            if (existing != null && !existing.IsRevoked)
            {
                existing.IsRevoked = true;
                existing.RevokedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task SendPhoneOtpAsync(Guid accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new InvalidOperationException("Account not found.");
            if (string.IsNullOrWhiteSpace(account.Phone))
                throw new InvalidOperationException("No phone number linked to this account.");
            if (account.PhoneVerified)
                throw new InvalidOperationException("Phone number is already verified.");

            var otp = await _phoneOtpService.GenerateAndSaveOtpAsync(account.Id);
            await _smsSender.SendOtpAsync(account.Phone, otp);
        }

        public async Task VerifyPhoneOtpAsync(Guid accountId, string otpInput)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new InvalidOperationException("Account not found.");

            var otpRecord = await _phoneOtpRepository.GetLatestUnusedAsync(accountId);
            if (otpRecord == null) throw new InvalidOperationException("OTP not found.");
            if (otpRecord.ExpiresAt < DateTimeOffset.UtcNow) throw new InvalidOperationException("OTP expired.");

            var isValid = BCrypt.Net.BCrypt.Verify(otpInput, otpRecord.OtpHash);
            if (!isValid) throw new InvalidOperationException("Invalid OTP.");

            account.PhoneVerified = true;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            otpRecord.UsedAt = DateTimeOffset.UtcNow;

            await _accountRepository.SaveChangesAsync();
        }

        // Add these methods to AuthService class

        public async Task RequestPasswordResetAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            // 1. Find account
            var account = await _accountRepository.GetByEmailAsync(normalizedEmail);

            // Note: For security reasons, it's best practice to return success even if the email
            // doesn't exist to prevent email enumeration. However, for a school project,
            // throwing an exception is easier to debug.
            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            // 2. Generate OTP and save hash
            var otp = await _emailOtpService.GenerateAndSaveOtpAsync(account.Id);

            // 3. Send OTP email
            await _emailSender.SendOtpAsync(account.Email, otp);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            // 1. Find account
            var account = await _accountRepository.GetByEmailAsync(normalizedEmail);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            // 2. Get latest unused OTP
            var emailOtp = await _emailOtpRepository.GetLatestUnusedAsync(account.Id);
            if (emailOtp == null)
            {
                throw new InvalidOperationException("No active OTP found for this account. Please request a new one.");
            }

            // 3. Check expiration
            if (emailOtp.ExpiresAt < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("OTP has expired. Please request a new one.");
            }

            // 4. Verify OTP
            var isValid = BCrypt.Net.BCrypt.Verify(request.Otp, emailOtp.OtpHash);
            if (!isValid)
            {
                throw new InvalidOperationException("Invalid OTP.");
            }

            // 5. Update password hash
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.UpdatedAt = DateTimeOffset.UtcNow;

            // 6. Mark OTP as used
            emailOtp.UsedAt = DateTimeOffset.UtcNow;

            // 7. Save changes (saves both Account and EmailOtp)
            await _accountRepository.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(Guid accountId, string currentPassword, string newPassword)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, account.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                throw new InvalidOperationException("Password must be at least 8 characters long.");
            }

            if (BCrypt.Net.BCrypt.Verify(newPassword, account.PasswordHash))
            {
                throw new InvalidOperationException("New password must be different from the current password.");
            }

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _accountRepository.SaveChangesAsync();
        }
    }
}