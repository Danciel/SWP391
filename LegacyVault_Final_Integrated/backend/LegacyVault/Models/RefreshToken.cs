namespace LegacyVault.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        // Navigation property
        public Account Account { get; set; } = null!;
    }
}