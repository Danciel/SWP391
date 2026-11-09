using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class AddWillAssetRequest
    {
        [Required]
        public Guid AssetId { get; set; }
    }
}
