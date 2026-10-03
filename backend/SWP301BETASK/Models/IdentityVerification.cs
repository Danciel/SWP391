namespace SWP301BETASK.Models
{
    public class IdentityVerification
    {
        public Guid Id { get; set; }

        public Guid AccountId { get; set; }

        // MOCK / AWS / AZURE /...
        public string Provider { get; set; } = "MOCK";

        // ID trả về từ hệ thống xác minh bên thứ ba
        public string? ExternalIdentityId { get; set; }

        // PENDING / VERIFIED / FAILED
        public string Status { get; set; } = "PENDING";

        public string? FailureReason { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation Property
        public Account Account { get; set; } = null!;
    }
}