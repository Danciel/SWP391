using LegacyVault.Features.IdentityAccess.DTOs;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IAuthService
    {
        Task<AccountResponse> RegisterAsync(RegisterRequest request);

        Task VerifyEmailOtpAsync(VerifyEmailOtpRequest request);

        Task ResendEmailOtpAsync(ResendEmailOtpRequest request);

        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}