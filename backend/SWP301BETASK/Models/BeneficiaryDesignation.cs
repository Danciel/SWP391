namespace SWP301BETASK.Models
{
    public class BeneficiaryDesignation
    {
        public Guid Id { get; set; }

        // Thuộc Will nào
        public Guid WillId { get; set; }

        // Nếu beneficiary đã có account trong hệ thống thì link vào
        public Guid? BeneficiaryAccountId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Relationship { get; set; }

        // DESIGNATED / LINKED / REVOKED
        public string Status { get; set; } = "DESIGNATED";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public Will Will { get; set; } = null!;

        public Account? BeneficiaryAccount { get; set; }
    }
}