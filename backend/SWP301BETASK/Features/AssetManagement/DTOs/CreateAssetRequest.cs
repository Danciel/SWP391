using System.ComponentModel.DataAnnotations;

namespace SWP301BETASK.Features.AssetManagement.DTOs
{
    public class CreateAssetRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string AssetType { get; set; } = string.Empty;

        public string? Provider { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Note { get; set; }
    }
}