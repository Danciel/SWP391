namespace SWP301BETASK.Models
{
    public class AssetVerification
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }

        public string Provider { get; set; } = string.Empty;

        public string? ExternalIdentifier { get; set; }

        public string Status { get; set; } = "PENDING";

        public string? FailureReason { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public Asset Asset { get; set; } = null!;
    }
}