namespace LegacyVault.Models
{
    public class ExecutorAssignment
    {
        public Guid Id { get; set; }

        public Guid WillId { get; set; }

        public Guid ExecutorAccountId { get; set; }

        public string Status { get; set; } = "PENDING";

        public DateTimeOffset AssignedAt { get; set; }

        public DateTimeOffset? RespondedAt { get; set; }

        public Will? Will { get; set; }

        public Account? ExecutorAccount { get; set; }
    }
}
