namespace LegacyVault.Models
{
    public class Account
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "USER";

        public bool EmailVerified { get; set; }

        public bool PhoneVerified { get; set; }

        public string IdentityStatus { get; set; } = "UNVERIFIED";

        public string? ExternalIdentityId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}