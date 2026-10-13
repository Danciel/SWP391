namespace LegacyVault.Models
{
    public class AssetVerification
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }

        public string? Provider { get; set; }

        public string? ExternalIdentifier { get; set; }

        public string Status { get; set; } = "PENDING";

        public string? FailureReason { get; set; }

        public DateTimeOffset? VerifiedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Asset? Asset { get; set; }
    }
}
