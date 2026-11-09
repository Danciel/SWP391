namespace LegacyVault.Models
{
    public class DmsConfiguration
    {
        public Guid Id { get; set; }

        public Guid WillId { get; set; }

        public bool Enabled { get; set; }

        public int CheckInIntervalDays { get; set; }

        public int ReminderBeforeDays { get; set; }

        public int MaxMissedCheckIns { get; set; }

        public int MissedCheckIns { get; set; }

        public DateTimeOffset? LastCheckInAt { get; set; }

        public DateTimeOffset? NextCheckInAt { get; set; }

        // D01: extra days after a due date before a cycle counts as missed.
        public int GraceDays { get; set; } = 2;

        // D03: due date we already reminded for (idempotency).
        public DateTimeOffset? LastReminderForDueAt { get; set; }

        // D05: when the missed threshold was reached (review case marker).
        public DateTimeOffset? ThresholdReachedAt { get; set; }

        // D06: when the Executor was told (idempotency).
        public DateTimeOffset? ExecutorNotifiedAt { get; set; }

        public string Status { get; set; } = "WAITING";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Will? Will { get; set; }

        public ICollection<DmsCheckIn> CheckIns { get; set; }
            = new List<DmsCheckIn>();
    }
}
