namespace LegacyVault.Features.WillManagement.Services
{
    // Pure logic (no DB) so it is easy to unit-test and explain.
    public static class DmsCycleCalculator
    {
        // R03 (pending): default grace period after a due date.
        public const int DefaultGraceDays = 2;

        // Returns the ScheduledAt of every cycle that was missed.
        // A cycle starting at dueAt is missed once now > dueAt + grace.
        public static List<DateTimeOffset> GetMissedCycles(
            DateTimeOffset nextDueAt,
            int intervalDays,
            int graceDays,
            DateTimeOffset now,
            int maxToReturn)
        {
            var result = new List<DateTimeOffset>();
            var due = nextDueAt;

            while (result.Count < maxToReturn &&
                   now > due.AddDays(graceDays))
            {
                result.Add(due);
                due = due.AddDays(intervalDays);
            }

            return result;
        }
    }
}
