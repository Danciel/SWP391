using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class CreateAssetAssignmentRequest
    {
        [Required]
        public Guid WillAssetId { get; set; }

        [Required]
        public Guid BeneficiaryDesignationId { get; set; }
    }
}
