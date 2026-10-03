namespace SWP301BETASK.Models
{
    public class AssetAssignment
    {
        public Guid Id { get; set; }

        // Asset nào trong Will
        public Guid WillAssetId { get; set; }

        // Giao cho Beneficiary nào
        public Guid BeneficiaryDesignationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public WillAsset WillAsset { get; set; } = null!;

        public BeneficiaryDesignation BeneficiaryDesignation { get; set; } = null!;
    }
}