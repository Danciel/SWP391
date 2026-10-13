using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class ConfigureDmsRequest
    {
        public bool Enabled { get; set; } = true;

        [Range(1, 3650)]
        public int CheckInIntervalDays { get; set; } = 30;

        [Range(0, 3649)]
        public int ReminderBeforeDays { get; set; } = 3;

        [Range(1, 100)]
        public int MaxMissedCheckIns { get; set; } = 3;
    }
}
