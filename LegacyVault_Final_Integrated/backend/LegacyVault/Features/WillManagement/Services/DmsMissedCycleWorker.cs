using LegacyVault.Data;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // D04: counts missed check-in cycles. Safe to run repeatedly
    // and after downtime: each cycle is recorded once (MISSED row)
    // and NextCheckInAt moves forward, so nothing is counted twice.
    // At the threshold it only sets THRESHOLD_REACHED. It never marks
    // anyone deceased or starts handover (R04); D05/D06 build on this.
    public class DmsMissedCycleWorker : BackgroundService
    {
        private static readonly TimeSpan Period =
            TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DmsMissedCycleWorker> _logger;

        public DmsMissedCycleWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<DmsMissedCycleWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider
                        .GetRequiredService<LegacyVaultDbContext>();
                    await EvaluateAsync(db, DateTimeOffset.UtcNow,
                        stoppingToken);
                }
                catch (Exception ex) when (
                    ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "DMS evaluation failed.");
                }

                try { await Task.Delay(Period, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }

        // Public so a test or an admin endpoint can call it directly.
        public static async Task<int> EvaluateAsync(
            LegacyVaultDbContext db,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var configs = await db.DmsConfigurations
                .Include(x => x.Will)
                .Where(x =>
                    x.Enabled &&
                    x.Status == "ACTIVE" &&
                    x.NextCheckInAt != null &&
                    x.Will!.Status == "ACTIVE")
                .ToListAsync(ct);

            var changed = 0;

            foreach (var dms in configs)
            {
                var room = Math.Max(
                    0, dms.MaxMissedCheckIns - dms.MissedCheckIns);

                var missed = DmsCycleCalculator.GetMissedCycles(
                    dms.NextCheckInAt!.Value,
                    dms.CheckInIntervalDays,
                    DmsCycleCalculator.DefaultGraceDays,
                    now,
                    room);

                if (missed.Count == 0) continue;

                var existing = await db.DmsCheckIns
                    .Where(c => c.DmsConfigurationId == dms.Id &&
                                c.Status == "MISSED")
                    .Select(c => c.ScheduledAt)
                    .ToListAsync(ct);

                foreach (var scheduledAt in missed)
                {
                    if (existing.Contains(scheduledAt)) continue;

                    db.DmsCheckIns.Add(new DmsCheckIn
                    {
                        Id = Guid.NewGuid(),
                        DmsConfigurationId = dms.Id,
                        ScheduledAt = scheduledAt,
                        CheckedInAt = null,
                        Status = "MISSED",
                        CreatedAt = now
                    });
                    dms.MissedCheckIns++;
                }

                dms.NextCheckInAt = dms.NextCheckInAt.Value
                    .AddDays(dms.CheckInIntervalDays * missed.Count);
                dms.UpdatedAt = now;

                if (dms.MissedCheckIns >= dms.MaxMissedCheckIns)
                {
                    dms.Status = "THRESHOLD_REACHED";
                }

                changed++;
            }

            if (changed > 0) await db.SaveChangesAsync(ct);
            return changed;
        }
    }
}
