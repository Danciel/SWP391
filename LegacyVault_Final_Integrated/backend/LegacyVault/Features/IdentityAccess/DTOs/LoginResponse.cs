namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public AccountResponse User { get; set; } = new();
    }
}