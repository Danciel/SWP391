namespace LegacyVault.Models
{
    public class Asset
    {
        public Guid Id { get; set; }

        public Guid OwnerAccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string AssetType { get; set; } = string.Empty;

        public string? Provider { get; set; }

        public string Status { get; set; } = "ACTIVE";

        public string VerificationStatus { get; set; } = "UNVERIFIED";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Account? OwnerAccount { get; set; }

        public AssetSecret? Secret { get; set; }

        public ICollection<AssetVerification> Verifications { get; set; }
            = new List<AssetVerification>();
    }
}
