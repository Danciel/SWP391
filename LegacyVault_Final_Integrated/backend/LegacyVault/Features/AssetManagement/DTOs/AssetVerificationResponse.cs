namespace LegacyVault.Features.AssetManagement.DTOs
{
    public class AssetVerificationResponse
    {
        public Guid AssetId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Message { get; set; }
    }
}
