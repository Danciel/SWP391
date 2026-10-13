namespace SWP301BETASK.Features.WillManagement.Models
{
    public class ExecutorAssignment
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Will Will { get; set; } = null!;
        public Guid ExecutorProfileId { get; set; }
        public ExecutorProfile Profile { get; set; } = null!;
        public string Status { get; set; } = ExecutorAssignmentStatuses.Selected;
        public decimal Fee { get; set; }
        public string PaymentStatus { get; set; } = PaymentStatuses.None;
        public DateTime? RequestedAt { get; set; }
        public DateTime? DeadlineAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
