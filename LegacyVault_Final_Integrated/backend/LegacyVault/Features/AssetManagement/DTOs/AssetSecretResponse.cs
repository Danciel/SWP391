namespace LegacyVault.Features.AssetManagement.DTOs
{
    public class AssetSecretResponse
    {
        public Guid AssetId { get; set; }

        public Dictionary<string, string> SecretData { get; set; } = new();
    }
}
