namespace LegacyVault.Models
{
    public static class AssetStatuses
    {
        public const string Active = "ACTIVE";

        // The handover is COMPLETED: the asset left the Owner's vault. The row (and its
        // encrypted secret) stays because beneficiaries read it, but the Owner can no
        // longer list, reveal, edit, verify, delete or reuse it.
        public const string HandedOver = "HANDED_OVER";
    }

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
