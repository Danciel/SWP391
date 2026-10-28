using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.AssetManagement.DTOs
{
    public class UpdateAssetRequest
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string AssetType { get; set; } = string.Empty;

        public string? Provider { get; set; }

        // null => keep current encrypted secret
        public Dictionary<string, string>? SecretData { get; set; }

        // Only for a verified Gmail/GitHub asset; null or empty means preserve existing.
        [MaxLength(1024)]
        public string? HandoverPassword { get; set; }

        public bool HandoverConsent { get; set; }
    }
}
