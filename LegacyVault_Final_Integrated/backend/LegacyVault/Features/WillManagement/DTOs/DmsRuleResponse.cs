namespace LegacyVault.Features.WillManagement.DTOs
{
    public class DmsRuleResponse
    {
        public Guid Id { get; set; }
        public int ReminderBeforeDays { get; set; }
        public int MaxMissedCheckIns { get; set; }
        public int GraceDays { get; set; }
        public int MinCheckInIntervalDays { get; set; }
        public int MaxCheckInIntervalDays { get; set; }
        public Guid? UpdatedByAccountId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}