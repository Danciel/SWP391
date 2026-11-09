namespace LegacyVault.Features.AssetManagement.DTOs
{
    public class AssetResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string AssetType { get; set; } = string.Empty;

        public string? Provider { get; set; }

        public string Status { get; set; } = string.Empty;

        public string VerificationStatus { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
