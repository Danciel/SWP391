using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // C6. After the DMS reached its threshold (TRIGGERED) the Owner can still
    // prove they are alive, as long as no handover has started:
    //   * DMS goes back to ACTIVE with a fresh check-in cycle
    //   * pending / approved death certificates are cancelled
    //   * the Executor is told
    //   * an account lock placed by an approved certificate is lifted (N7)
    // The LifeStatus source stays the authority: if it says DECEASED the
    // request is refused.
    public class OwnerAliveService : IOwnerAliveService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly ILifeStatusClient _lifeStatus;
        private readonly IAccountLockService _lock;
        private readonly IEmailSender _email;
        private readonly ILogger<OwnerAliveService> _logger;

        public OwnerAliveService(
            LegacyVaultDbContext db,
            ILifeStatusClient lifeStatus,
            IAccountLockService accountLock,
            IEmailSender email,
            ILogger<OwnerAliveService> logger)
        {
            _db = db;
            _lifeStatus = lifeStatus;
            _lock = accountLock;
            _email = email;
            _logger = logger;
        }

        public async Task<OwnerAliveResponse> ConfirmAliveAsync(
            Guid willId,
            Guid ownerAccountId,
            CancellationToken ct = default)
        {
            var will = await _db.Wills
                .Include(x => x.DmsConfiguration)
                .Include(x => x.OwnerAccount)
                .Include(x => x.ExecutorAssignment)
                    .ThenInclude(e => e!.ExecutorAccount)
                .FirstOrDefaultAsync(x =>
                    x.Id == willId && x.OwnerAccountId == ownerAccountId, ct)
                ?? throw new KeyNotFoundException("Will not found.");

            var dms = will.DmsConfiguration;

            if (dms == null || !dms.Enabled)
                throw new InvalidOperationException(
                    "DMS is not enabled for this will.");

            if (dms.Status != "TRIGGERED")
            {
                throw new InvalidOperationException(
                    "I'm alive is only for a triggered DMS. Use the normal check-in.");
            }

            if (will.Status != "ACTIVE")
            {
                throw new WillConflictException(
                    "A handover already started for this will, so I'm alive " +
                    "can no longer cancel it. Contact support.");
            }

            string life;
            try
            {
                life = await _lifeStatus.GetStatusAsync(ownerAccountId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "LifeStatus lookup failed for {OwnerId}.", ownerAccountId);
                life = "UNKNOWN";
            }

            if (string.Equals(life, "DECEASED", StringComparison.OrdinalIgnoreCase))
            {
                throw new WillConflictException(
                    "The identity provider reports this account as deceased. " +
                    "Contact support.");
            }

            var now = DateTimeOffset.UtcNow;

            // Same effect as a normal check-in.
            _db.DmsCheckIns.Add(new DmsCheckIn
            {
                Id = Guid.NewGuid(),
                DmsConfigurationId = dms.Id,
                ScheduledAt = dms.NextCheckInAt ?? now,
                CheckedInAt = now,
                Status = "CHECKED_IN",
                CreatedAt = now
            });

            dms.Status = "ACTIVE";
            dms.LastCheckInAt = now;
            dms.NextCheckInAt = now.AddDays(dms.CheckInIntervalDays);
            dms.MissedCheckIns = 0;
            dms.ThresholdReachedAt = null;
            dms.ExecutorNotifiedAt = null;
            dms.LastReminderForDueAt = null;
            dms.UpdatedAt = now;

            var hadApproved = await _db.LegalCases.AnyAsync(x =>
                x.WillId == willId && x.Status == "APPROVED", ct);

            await _db.SaveChangesAsync(ct);

            var cancelled = await _db.LegalCases
                .Where(x => x.WillId == willId &&
                            (x.Status == "PENDING" || x.Status == "APPROVED"))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "CANCELLED")
                    .SetProperty(x => x.ReviewedByAccountId, ownerAccountId)
                    .SetProperty(x => x.ReviewNote,
                        "Cancelled: the Owner confirmed they are alive.")
                    .SetProperty(x => x.ReviewedAt, now), ct);

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = ownerAccountId,
                Action = "OWNER_CONFIRMED_ALIVE",
                EntityType = "DmsConfiguration",
                EntityId = willId,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    lifeStatus = life,
                    cancelledLegalCases = cancelled
                }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync(ct);

            if (hadApproved)
            {
                try
                {
                    await _lock.UnlockOwnerAsync(
                        ownerAccountId, "Owner confirmed alive", ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Owner unlock failed for {OwnerId}.", ownerAccountId);
                }
            }

            var notified = false;
            var assignment = will.ExecutorAssignment;
            var executor = assignment?.ExecutorAccount;

            if (assignment?.Status == "ACCEPTED" && executor != null)
            {
                try
                {
                    await _email.SendOwnerAliveConfirmedEmailAsync(
                        executor.Email,
                        executor.FullName,
                        will.OwnerAccount?.FullName ?? string.Empty,
                        will.Title);
                    notified = true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Executor alive e-mail failed for will {WillId}.", willId);
                }
            }

            return new OwnerAliveResponse
            {
                WillId = willId,
                DmsStatus = dms.Status,
                NextCheckInAt = dms.NextCheckInAt,
                CancelledLegalCases = cancelled,
                ExecutorNotified = notified
            };
        }
    }
}
