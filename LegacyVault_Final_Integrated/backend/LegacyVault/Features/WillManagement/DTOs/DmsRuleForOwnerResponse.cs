namespace LegacyVault.Features.WillManagement.DTOs
{
    public class DmsRuleForOwnerResponse
    {
        public int ReminderBeforeDays { get; set; }
        public int MaxMissedCheckIns { get; set; }
        public int MinCheckInIntervalDays { get; set; }
        public int MaxCheckInIntervalDays { get; set; }
    }
}