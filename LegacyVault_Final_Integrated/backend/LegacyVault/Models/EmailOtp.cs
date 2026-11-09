namespace LegacyVault.Models
{
    public class EmailOtp
    {
        public Guid Id { get; set; }

        public Guid AccountId { get; set; }

        public string OtpHash { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? UsedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Account Account { get; set; } = null!;
    }
}