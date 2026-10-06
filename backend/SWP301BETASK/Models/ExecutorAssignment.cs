namespace SWP301BETASK.Models
{
    public class ExecutorAssignment
    {
        public Guid Id { get; set; }

        // Will nào đang chọn Executor này
        public Guid WillId { get; set; }

        // Executor là một Account có Role = EXECUTOR
        public Guid ExecutorAccountId { get; set; }

        // PENDING / ACTIVE / DECLINED / REMOVED
        public string Status { get; set; } = "PENDING";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }


        // Navigation
        public Will Will { get; set; } = null!;

        public Account ExecutorAccount { get; set; } = null!;
    }
}