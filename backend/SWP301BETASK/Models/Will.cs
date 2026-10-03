namespace SWP301BETASK.Models
{
    public class Will
    {
        public Guid Id { get; set; }

        // Chủ sở hữu Will
        public Guid OwnerAccountId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // DRAFT / PENDING_EXECUTOR / ACTIVE /
        // NEEDS_EXECUTOR / REVOKED / CLOSED
        public string Status { get; set; } = "DRAFT";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ActivatedAt { get; set; }

        // Navigation
        public Account OwnerAccount { get; set; } = null!;
    }
}