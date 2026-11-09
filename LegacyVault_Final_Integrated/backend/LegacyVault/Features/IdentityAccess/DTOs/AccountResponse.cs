namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class AccountResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool EmailVerified { get; set; }

        public bool PhoneVerified { get; set; }

        public string IdentityStatus { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}