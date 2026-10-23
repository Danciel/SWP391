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
                if (s != "ACTIVE" && s != "THRESHOLD_REACHED")
                    return BadRequest(new
                    { message = "Status must be ACTIVE or THRESHOLD_REACHED." });

                dms.Status = s;
                dms.ExecutorNotifiedAt = null;
                dms.ThresholdReachedAt =
                    s == "THRESHOLD_REACHED" ? now : null;
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
