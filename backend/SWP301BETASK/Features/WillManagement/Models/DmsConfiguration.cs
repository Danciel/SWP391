namespace SWP301BETASK.Features.WillManagement.Models
{
    public class DmsConfiguration
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Will Will { get; set; } = null!;
        public int IntervalDays { get; set; } = 30;
        public int ReminderDays { get; set; } = 3;
        public int MaxMissed { get; set; } = 2;
        public int MissedCount { get; set; }
        public DateTime? LastCheckInAt { get; set; }
        public DateTime? NextDueAt { get; set; }
        public string Status { get; set; } = DmsStatuses.Waiting;
    }
}
