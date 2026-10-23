using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class CreateAssetAssignmentRequest
    {
        [Required]
        public Guid WillAssetId { get; set; }

        [Required]
        public Guid BeneficiaryDesignationId { get; set; }

        // Share of this specific asset allocated to the beneficiary.
        // Defaults to 100 for compatibility with existing single-beneficiary clients.
        [Range(typeof(decimal), "0.01", "100.00")]
        public decimal AllocationPercentage { get; set; } = 100m;
    }
}
