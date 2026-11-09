namespace LegacyVault.Models
{
    public class BeneficiaryDesignation
    {
        public Guid Id { get; set; }

        public Guid WillId { get; set; }

        public Guid? BeneficiaryAccountId { get; set; }

        public string FullName { get; set; } = string.Empty;

        // Current V1 schema requires Email.
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Relationship { get; set; }

        // LV-15: optional ID document number for an unregistered person.
        public string? IdentityNumber { get; set; }

        public string Status { get; set; } = "DESIGNATED";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Will? Will { get; set; }

        public Account? BeneficiaryAccount { get; set; }

        public ICollection<AssetAssignment> AssetAssignments { get; set; }
            = new List<AssetAssignment>();
    }
}
