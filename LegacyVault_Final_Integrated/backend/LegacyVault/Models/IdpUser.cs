namespace LegacyVault.Models
{
    public class IdpUser
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string IdentityStatus { get; set; } = "UNVERIFIED";

        public Guid? FaceId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
