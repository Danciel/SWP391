using LegacyVault.Data;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers
{
    // LV-24/26: lets the Owner UI show "check-in due" and "Miss 1/3"
    // without a notification table (reminders are also e-mailed).
    [ApiController]
    [Route("api/dms")]
    [Authorize]
    public class DmsController : ControllerBase
    {
        private readonly LegacyVaultDbContext _db;
        private readonly ICurrentUserService _user;

        public DmsController(
            LegacyVaultDbContext db,
            ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> Alerts()
        {
            var me = _user.GetRequiredAccountId();
            var now = DateTimeOffset.UtcNow;

            var rows = await _db.DmsConfigurations.AsNoTracking()
                .Where(x => x.Enabled &&
                            x.Will!.OwnerAccountId == me &&
                            (x.Status == "ACTIVE" ||
                             x.Status == "THRESHOLD_REACHED"))
                .Select(x => new
                {
                    willId = x.WillId,
                    willTitle = x.Will!.Title,
                    status = x.Status,
                    nextCheckInAt = x.NextCheckInAt,
                    missedCheckIns = x.MissedCheckIns,
                    maxMissedCheckIns = x.MaxMissedCheckIns,
                    reminderBeforeDays = x.ReminderBeforeDays
                })
                .ToListAsync();

            return Ok(rows.Select(x => new
            {
                x.willId,
                x.willTitle,
                x.status,
                x.nextCheckInAt,
                x.missedCheckIns,
                x.maxMissedCheckIns,
                checkInDueSoon = x.nextCheckInAt != null &&
                    now >= x.nextCheckInAt.Value.AddDays(-x.reminderBeforeDays),
                label = x.missedCheckIns > 0
                    ? $"Miss {x.missedCheckIns}/{x.maxMissedCheckIns}"
                    : null
            }));
        }
    }
}
