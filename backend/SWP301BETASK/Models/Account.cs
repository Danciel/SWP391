namespace SWP301BETASK.Models
{
    public class Account
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "USER";

        public bool EmailVerified { get; set; } = false;

        public bool PhoneVerified { get; set; } = false;

        public string IdentityStatus { get; set; } = "UNVERIFIED";

        public string? ExternalIdentityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}