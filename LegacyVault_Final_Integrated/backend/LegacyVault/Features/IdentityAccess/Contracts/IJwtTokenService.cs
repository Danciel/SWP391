using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IJwtTokenService
    {
        string GenerateToken(
            Account account,
            bool rememberMe);
    }
}