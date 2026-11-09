namespace LegacyVault.Models
{
    // LV-22: one review per completed Will.
    public class ExecutorReview
    {
        public Guid Id { get; set; }
        public Guid ExecutorAccountId { get; set; }
        public Guid OwnerAccountId { get; set; }
        public Guid WillId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Account? OwnerAccount { get; set; }
    }
}
