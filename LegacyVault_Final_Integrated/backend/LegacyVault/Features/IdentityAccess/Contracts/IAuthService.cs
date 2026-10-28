using LegacyVault.Features.IdentityAccess.DTOs;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IAuthService
    {
        Task<AccountResponse> RegisterAsync(RegisterRequest request);

        Task VerifyEmailOtpAsync(VerifyEmailOtpRequest request);

        Task ResendEmailOtpAsync(ResendEmailOtpRequest request);

        Task<LoginResponse> LoginAsync(LoginRequest request);

        Task SendPhoneOtpAsync(Guid accountId); // or pass email to find account

        Task VerifyPhoneOtpAsync(Guid accountId, string otp);

        Task RequestPasswordResetAsync(string email);

        Task ResetPasswordAsync(ResetPasswordRequest request);

        Task ChangePasswordAsync(Guid accountId, string currentPassword, string newPassword);
    }
}