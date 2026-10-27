using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // Hourly DMS job (D03 reminder, D04 missed cycles, D05 threshold,
    // D06 executor email). Safe to run repeatedly and after downtime:
    // every step records what it already did in the database.
    // It never marks anyone deceased or starts handover (R04).
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
                    var sp = scope.ServiceProvider;
                    await EvaluateAsync(
                        sp.GetRequiredService<LegacyVaultDbContext>(),
                        sp.GetRequiredService<IEmailSender>(),
                        sp.GetRequiredService<ILifeStatusClient>(),
                        _logger,
                        DateTimeOffset.UtcNow,
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
        public static async Task EvaluateAsync(
            LegacyVaultDbContext db,
            IEmailSender email,
            ILifeStatusClient lifeStatus,
            ILogger logger,
            DateTimeOffset now,
            CancellationToken ct)
        {
            await CountMissedAndRemindAsync(
                db, email, logger, now, ct);

            await EscalateAsync(
                db, email, lifeStatus, logger, now, ct);
        }

        // D03 + D04
        private static async Task CountMissedAndRemindAsync(
            LegacyVaultDbContext db,
            IEmailSender email,
            ILogger logger,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var configs = await db.DmsConfigurations
                .Include(x => x.Will)
                    .ThenInclude(w => w!.OwnerAccount)
                .Where(x =>
                    x.Enabled &&
                    x.Status == "ACTIVE" &&
                    x.NextCheckInAt != null &&
                    x.Will!.Status == "ACTIVE")
                .ToListAsync(ct);

            foreach (var dms in configs)
            {
                // --- D04: record missed cycles (once each)
                var room = Math.Max(
                    0, dms.MaxMissedCheckIns - dms.MissedCheckIns);

                var missed = DmsCycleCalculator.GetMissedCycles(
                    dms.NextCheckInAt!.Value,
                    dms.CheckInIntervalDays,
                    dms.GraceDays,
                    now,
                    room);

                var newlyMissed = false;

                if (missed.Count > 0)
                {
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
                        newlyMissed = true;
                    }

                    dms.NextCheckInAt = dms.NextCheckInAt.Value
                        .AddDays(dms.CheckInIntervalDays * missed.Count);
                    dms.UpdatedAt = now;
                }

                if (dms.MissedCheckIns >= dms.MaxMissedCheckIns)
                {
                    // D05: threshold reached -> review marker only.
                    dms.Status = "TRIGGERED";
                    dms.ThresholdReachedAt ??= now;
                    await db.SaveChangesAsync(ct);
                    continue;
                }

                // --- D03: remind the Owner (below threshold only)
                var due = dms.NextCheckInAt!.Value;
                var inWindow =
                    newlyMissed ||
                    now >= due.AddDays(-dms.ReminderBeforeDays);

                if (inWindow && dms.LastReminderForDueAt != due)
                {
                    var owner = dms.Will?.OwnerAccount;
                    if (owner != null)
                    {
                        try
                        {
                            await email.SendDmsReminderEmailAsync(
                                owner.Email, owner.FullName, due);
                            dms.LastReminderForDueAt = due;
                        }
                        catch (Exception ex)
                        {
                            // Not marked as sent -> retried next hour.
                            logger.LogWarning(ex,
                                "DMS reminder failed for will {WillId}.",
                                dms.WillId);
                        }
                    }
                }

                await db.SaveChangesAsync(ct);
            }
        }

        // D05 + D06
        private static async Task EscalateAsync(
            LegacyVaultDbContext db,
            IEmailSender email,
            ILifeStatusClient lifeStatus,
            ILogger logger,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var configs = await db.DmsConfigurations
                .Include(x => x.Will)
                    .ThenInclude(w => w!.OwnerAccount)
                .Include(x => x.Will)
                    .ThenInclude(w => w!.ExecutorAssignment)
                        .ThenInclude(e => e!.ExecutorAccount)
                .Where(x =>
                    x.Status == "TRIGGERED" &&
                    x.ExecutorNotifiedAt == null &&
                    x.Will!.Status == "ACTIVE")
                .ToListAsync(ct);

            foreach (var dms in configs)
            {
                var will = dms.Will!;

                // D05: ask the LifeStatus source; we only log it.
                // DMS never writes DECEASED and never hands over.
                var life = await lifeStatus.GetStatusAsync(
                    will.OwnerAccountId, ct);

                logger.LogInformation(
                    "DMS threshold for will {WillId}, LifeStatus={Life}.",
                    will.Id, life);

                // D06: only an ACCEPTED executor is told.
                var assignment = will.ExecutorAssignment;
                var executor = assignment?.ExecutorAccount;

                if (assignment == null ||
                    assignment.Status != "ACCEPTED" ||
                    executor == null ||
                    will.OwnerAccount == null)
                {
                    continue;
                }

                try
                {
                    await email.SendDmsTriggerEmailAsync(
                        executor.Email,
                        executor.FullName,
                        will.OwnerAccount.FullName,
                        will.Title);

                    dms.ExecutorNotifiedAt = now;
                    dms.UpdatedAt = now;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Executor email failed for will {WillId}.",
                        will.Id);
                }
            }
        }
    }
}
