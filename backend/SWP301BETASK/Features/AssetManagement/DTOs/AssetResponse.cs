namespace SWP301BETASK.Features.AssetManagement.DTOs
{
    public class AssetResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public string? Provider { get; set; }

        public string VerificationStatus { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Username { get; set; }

        public string? Note { get; set; }

        public bool HasEncryptedSecret { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}