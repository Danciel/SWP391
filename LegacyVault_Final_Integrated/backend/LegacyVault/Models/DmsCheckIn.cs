namespace LegacyVault.Models
{
    public class DmsCheckIn
    {
        public Guid Id { get; set; }

        public Guid DmsConfigurationId { get; set; }

        public DateTimeOffset ScheduledAt { get; set; }

        public DateTimeOffset? CheckedInAt { get; set; }

        public string Status { get; set; } = "PENDING";

        public DateTimeOffset CreatedAt { get; set; }

        public DmsConfiguration? DmsConfiguration { get; set; }
    }
}
