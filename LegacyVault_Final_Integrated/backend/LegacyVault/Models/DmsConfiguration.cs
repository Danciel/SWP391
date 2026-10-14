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

        public string Status { get; set; } = "WAITING";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Will? Will { get; set; }

        public ICollection<DmsCheckIn> CheckIns { get; set; }
            = new List<DmsCheckIn>();
    }
}
