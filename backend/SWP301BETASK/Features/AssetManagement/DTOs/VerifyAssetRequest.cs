using System.ComponentModel.DataAnnotations;

namespace SWP301BETASK.Features.AssetManagement.DTOs
{
    public class VerifyAssetRequest
    {
        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}