using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class UpdateAssetAssignmentRequest
    {
        [Range(typeof(decimal), "0.01", "100.00")]
        public decimal AllocationPercentage { get; set; }
    }
}
