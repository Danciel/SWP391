using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Features.WillManagement.Services;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers
{
    // LV-38: demo tooling. Only ADMIN; normal users cannot reach it.
    [ApiController]
    [Route("api/admin/dms")]
    [Authorize(Roles = "ADMIN")]
    public class AdminDmsController : ControllerBase
    {
        private readonly LegacyVaultDbContext _db;
        private readonly IEmailSender _email;
        private readonly ILifeStatusClient _life;
        private readonly ILogger<AdminDmsController> _logger;
        private readonly ICurrentUserService _user;

        public AdminDmsController(
            LegacyVaultDbContext db,
            IEmailSender email,
            ILifeStatusClient life,
            ILogger<AdminDmsController> logger,
            ICurrentUserService user)
        {
            _db = db;
            _email = email;
            _life = life;
            _logger = logger;
            _user = user;
        }

        // Pick a demo will.
        [HttpGet]
        public async Task<IActionResult> List()
        {
            return Ok(await _db.DmsConfigurations.AsNoTracking()
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new
                {
                    willId = x.WillId,
                    willTitle = x.Will!.Title,
                    willStatus = x.Will.Status,
                    ownerName = x.Will.OwnerAccount != null
                        ? x.Will.OwnerAccount.FullName : "",
                    x.Enabled,
                    x.Status,
                    x.CheckInIntervalDays,
                    x.GraceDays,
                    x.MissedCheckIns,
                    x.MaxMissedCheckIns,
                    x.NextCheckInAt
                })
                .ToListAsync());
        }

        [HttpPost("{willId:guid}/configure")]
        public async Task<IActionResult> Configure(
            Guid willId,
            [FromBody] AdminDmsRequest request)
        {
            var dms = await _db.DmsConfigurations
                .FirstOrDefaultAsync(x => x.WillId == willId);

            if (dms == null)
                return NotFound(new { message = "DMS not found." });

            var now = DateTimeOffset.UtcNow;

            if (request.NextCheckInAt != null)
                dms.NextCheckInAt = request.NextCheckInAt;
            if (request.MissedCheckIns != null)
                dms.MissedCheckIns = request.MissedCheckIns.Value;
            if (request.CheckInIntervalDays != null)
                dms.CheckInIntervalDays = request.CheckInIntervalDays.Value;
            if (request.GraceDays != null)
                dms.GraceDays = request.GraceDays.Value;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var s = request.Status.Trim().ToUpperInvariant();

                // THRESHOLD_REACHED is the old name of TRIGGERED.
                if (s == "THRESHOLD_REACHED") s = "TRIGGERED";

                if (s != "ACTIVE" && s != "TRIGGERED")
                    return BadRequest(new
                    { message = "Status must be ACTIVE or TRIGGERED." });

                if (s == "TRIGGERED")
                {
                    DmsMissedCycleWorker.MarkTriggered(
                        _db, dms, now, "ADMIN_CONFIGURE",
                        _user.GetRequiredAccountId(), null);
                }
                else
                {
                    dms.Status = s;
                    dms.ExecutorNotifiedAt = null;
                    dms.ThresholdReachedAt = null;
                }
            }

            dms.UpdatedAt = now;
            _db.AuditLogs.Add(new LegacyVault.Models.AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = _user.GetRequiredAccountId(), Action = "ADMIN_DMS_CONFIGURED",
                EntityType = "DmsConfiguration", EntityId = willId,
                Details = System.Text.Json.JsonSerializer.Serialize(new { request.Status, request.MissedCheckIns, request.CheckInIntervalDays, request.GraceDays, request.NextCheckInAt }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync();
            return Ok(new { dms.WillId, dms.Status, dms.MissedCheckIns, dms.NextCheckInAt });
        }

        // Demo / support tool: force the trigger now (same state as missing all
        // check-ins), then tell the Executor. It does NOT mark the Owner deceased
        // and does NOT start a handover: the Executor still needs the certificate
        // and Legal Verify.
        [HttpPost("{willId:guid}/force-trigger")]
        public async Task<IActionResult> ForceTrigger(
            Guid willId,
            [FromBody] HandoverReasonRequest request)
        {
            var reason = request?.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { message = "A reason is required." });

            var dms = await _db.DmsConfigurations
                .Include(x => x.Will)
                .FirstOrDefaultAsync(x => x.WillId == willId);

            if (dms == null || !dms.Enabled)
                return NotFound(new { message = "This will has no active DMS." });

            if (dms.Will == null || dms.Will.Status != "ACTIVE")
            {
                return Conflict(new
                {
                    message = "The DMS can be triggered only for an ACTIVE will."
                });
            }

            if (dms.Status == "TRIGGERED")
                return Conflict(new { message = "The DMS is already triggered." });

            var now = DateTimeOffset.UtcNow;

            DmsMissedCycleWorker.MarkTriggered(
                _db, dms, now, "ADMIN_FORCED",
                _user.GetRequiredAccountId(), reason);
            await _db.SaveChangesAsync();

            // Same email the automatic trigger sends; a mail problem never undoes the trigger.
            try
            {
                await DmsMissedCycleWorker.NotifyExecutorAsync(
                    _db, _email, _life, _logger, willId, now,
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Executor email failed after forced trigger for will {WillId}.", willId);
            }

            var notified = await _db.DmsConfigurations.AsNoTracking()
                .Where(x => x.WillId == willId)
                .Select(x => x.ExecutorNotifiedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                willId,
                status = "TRIGGERED",
                executorNotified = notified != null,
                message = notified != null
                    ? "DMS triggered. The Executor was emailed."
                    : "DMS triggered. The Executor was not emailed (no accepted Executor, or the email failed); it will be retried every hour."
            });
        }

        // Run the same job as the hourly worker, optionally at a
        // simulated time, so a demo does not need to wait days.
        [HttpPost("evaluate")]
        public async Task<IActionResult> Evaluate(
            [FromBody] AdminEvaluateRequest request)
        {
            var simulatedNow = request.Now ?? DateTimeOffset.UtcNow;
            await DmsMissedCycleWorker.EvaluateAsync(
                _db, _email, _life, _logger,
                simulatedNow,
                HttpContext.RequestAborted);
            _db.AuditLogs.Add(new LegacyVault.Models.AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = _user.GetRequiredAccountId(), Action = "ADMIN_DMS_EVALUATED",
                EntityType = "DmsConfiguration", EntityId = null,
                Details = System.Text.Json.JsonSerializer.Serialize(new { now = simulatedNow, mode = request.Now.HasValue ? "SIMULATED" : "REAL_TIME" }),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync();

            return Ok(new { message = "DMS evaluation finished.", evaluatedAt = simulatedNow });
        }

    }
}
