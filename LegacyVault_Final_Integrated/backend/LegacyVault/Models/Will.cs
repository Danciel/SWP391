namespace LegacyVault.Models
{
    public class Will
    {
        public Guid Id { get; set; }

        public Guid OwnerAccountId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = "DRAFT";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ActivatedAt { get; set; }

        public Account? OwnerAccount { get; set; }

        public ICollection<WillAsset> WillAssets { get; set; }
            = new List<WillAsset>();

        public ICollection<BeneficiaryDesignation> Beneficiaries { get; set; }
            = new List<BeneficiaryDesignation>();

        public ExecutorAssignment? ExecutorAssignment { get; set; }

        public DmsConfiguration? DmsConfiguration { get; set; }
    }
}
