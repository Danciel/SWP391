namespace LegacyVault.Models
{
    public class AssetAssignment
    {
        public Guid Id { get; set; }

        public Guid WillAssetId { get; set; }

        public Guid BeneficiaryDesignationId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public WillAsset? WillAsset { get; set; }

        public BeneficiaryDesignation? BeneficiaryDesignation { get; set; }
    }
}
