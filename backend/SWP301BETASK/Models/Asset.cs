namespace SWP301BETASK.Models
{
    public class Asset
    {
        public Guid Id { get; set; }

        public Guid OwnerAccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public string? Provider { get; set; }

        public string Status { get; set; } = "ACTIVE";

        public string VerificationStatus { get; set; } = "UNVERIFIED";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        public Account OwnerAccount { get; set; } = null!;

        public AssetSecret? Secret { get; set; }

        public ICollection<AssetVerification> Verifications { get; set; }
            = new List<AssetVerification>();
    }
}