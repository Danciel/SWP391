namespace LegacyVault.Models
{
    public class PhoneOtp
        {
            public Guid Id { get; set; }
            public Guid AccountId { get; set; }
            public Account Account { get; set; } = null!;
            public string OtpHash { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAt { get; set; }
            public DateTimeOffset? UsedAt { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public int FailedAttempts { get; set; }
        }
}