using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class AssignAssetExecutorRequest
    {
        [Required]
        public Guid ExecutorAccountId { get; set; }
    }

    public class AssetExecutorItemResponse
    {
        public Guid WillAssetId { get; set; }

        public Guid ExecutorAccountId { get; set; }

        public string ExecutorName { get; set; } = string.Empty;

        // Designated by the Owner; acceptance and fee stay with the Will executor.
        public string Status { get; set; } = "ASSIGNED";
    }
}
