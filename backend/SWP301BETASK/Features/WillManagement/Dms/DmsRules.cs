using SWP301BETASK.Features.WillManagement.Models;

namespace SWP301BETASK.Features.WillManagement.Dms
{
    public static class DmsRules
    {
        public static readonly int[] AllowedIntervals = { 15, 30, 60, 90 };
        public static readonly int[] AllowedReminders = { 1, 3, 7 };
        public const int MinMissed = 1;
        public const int MaxMissedLimit = 3;

        public static List<string> Validate(int checkInDays, int reminderDays, int maxMissed)
        {
            var errors = new List<string>();
            if (!AllowedIntervals.Contains(checkInDays)) errors.Add($"checkInDays must be one of {string.Join(", ", AllowedIntervals)}");
            if (!AllowedReminders.Contains(reminderDays)) errors.Add($"reminderDays must be one of {string.Join(", ", AllowedReminders)}");
            if (maxMissed < MinMissed || maxMissed > MaxMissedLimit) errors.Add($"maxMissed must be between {MinMissed} and {MaxMissedLimit}");
            if (reminderDays >= checkInDays) errors.Add("reminderDays must be smaller than checkInDays");
            return errors;
        }

        public static bool IsValid(DmsConfiguration? d) =>
            d != null && Validate(d.IntervalDays, d.ReminderDays, d.MaxMissed).Count == 0;
    }
}
