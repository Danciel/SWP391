using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using LegacyVault.Features.IdentityAccess.Repositories;
using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailOtpService _emailOtpService;
        private readonly IEmailSender _emailSender;
        private readonly IEmailOtpRepository _emailOtpRepository;

        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(
    IAccountRepository accountRepository,
    IEmailOtpService emailOtpService,
    IEmailSender emailSender,
    IEmailOtpRepository emailOtpRepository,
    IJwtTokenService jwtTokenService)
        {
            _accountRepository = accountRepository;
            _emailOtpService = emailOtpService;
            _emailSender = emailSender;
            _emailOtpRepository = emailOtpRepository;
            _jwtTokenService = jwtTokenService;
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

            // 5. So sánh OTP user nhập với hash trong database
            var isValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Otp,
                    emailOtp.OtpHash);

            if (!isValid)
            {
                throw new InvalidOperationException(
                    "Invalid OTP.");
            }

            // 6. OTP đúng → xác nhận email
            account.EmailVerified = true;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            // 7. Đánh dấu OTP đã được sử dụng
            emailOtp.UsedAt = DateTimeOffset.UtcNow;

            // Account và EmailOtp đang được cùng DbContext theo dõi
            await _accountRepository.SaveChangesAsync();
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

            // 3. Sinh OTP mới và lưu hash xuống database
            var otp =
                await _emailOtpService
                    .GenerateAndSaveOtpAsync(account.Id);

            // 4. Gửi OTP mới qua Gmail
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

            // 4. Tạo JWT
            var token =
                _jwtTokenService.GenerateToken(
                    account,
                    request.RememberMe);

            // 5. Trả token + thông tin user
            return new LoginResponse
            {
                Token = token,

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
    }
}