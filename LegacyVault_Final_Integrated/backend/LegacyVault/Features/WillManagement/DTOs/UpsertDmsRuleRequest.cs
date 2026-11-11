using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class UpsertDmsRuleRequest
    {
        [Range(0, 3649)]
        public int ReminderBeforeDays { get; set; } = 3;

        [Range(1, 100)]
        public int MaxMissedCheckIns { get; set; } = 3;

        [Range(0, 30)]
        public int GraceDays { get; set; } = 2;

        [Range(1, 3650)]
        public int MinCheckInIntervalDays { get; set; } = 7;

        [Range(1, 3650)]
        public int MaxCheckInIntervalDays { get; set; } = 180;
    }
}