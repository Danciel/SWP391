namespace LegacyVault.Models
{
    public class DmsRule
    {
        // Singleton: fixed PK guarantees at most one row.
        public static readonly Guid SingletonId =
            Guid.Parse("00000000-0000-0000-0000-000000000001");

        public Guid Id { get; set; }

        // Locked from owners (visible to owners, read-only).
        public int ReminderBeforeDays { get; set; }
        public int MaxMissedCheckIns { get; set; }

        // Hidden from owners entirely.
        public int GraceDays { get; set; }

        // Owner picks CheckInIntervalDays within [Min, Max].
        public int MinCheckInIntervalDays { get; set; }
        public int MaxCheckInIntervalDays { get; set; }

        public Guid? UpdatedByAccountId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}