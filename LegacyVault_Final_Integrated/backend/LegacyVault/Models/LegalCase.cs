namespace LegacyVault.Models
{
    // LV-29/30: death certificate submitted by an Executor for Legal review.
    public class LegalCase
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Guid ExecutorAccountId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING"; // PENDING | APPROVED | REJECTED
        public Guid? ReviewedByAccountId { get; set; }
        public string? ReviewNote { get; set; }
        // JSON typed by the Executor on the death-certificate form (optional).
        public string? CertificateData { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public Will? Will { get; set; }
        public Account? ExecutorAccount { get; set; }
    }
}
