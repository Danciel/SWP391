using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Features.WillManagement.Services;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Dms
{
    public class DmsService : IDmsService
    {
        private readonly LegacyVaultDbContext _db;
        public DmsService(LegacyVaultDbContext db) => _db = db;

        public async Task<WillDto> SetAsync(Guid ownerId, Guid willId, SetDmsRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.DmsEditable, "Dead Man's Switch cannot be changed in the current will status");

            var errors = DmsRules.Validate(req.CheckInDays, req.ReminderDays, req.MaxMissed);
            if (errors.Count > 0) throw ApiException.BadRequest("Invalid DMS configuration", errors);

            var dms = will.Dms;
            if (dms == null)
            {
                dms = new DmsConfiguration { Id = Guid.NewGuid(), WillId = will.Id };
                will.Dms = dms;
                _db.DmsConfigurations.Add(dms);
            }
            dms.IntervalDays = req.CheckInDays;
            dms.ReminderDays = req.ReminderDays;
            dms.MaxMissed = req.MaxMissed;
            if (dms.Status == DmsStatuses.Active)
                dms.NextDueAt = (dms.LastCheckInAt ?? DateTime.UtcNow).AddDays(dms.IntervalDays);

            return await SaveAsync(will, ct);
        }

        public async Task<WillDto> CheckInAsync(Guid ownerId, Guid willId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            if (will.Status != WillStatus.Active)
                throw ApiException.Conflict("Check-in is only available for an active will");

            Restart(will.Dms ?? throw ApiException.Conflict("DMS is not configured"), DateTime.UtcNow);
            return await SaveAsync(will, ct);
        }

        /// <summary>Owner asks for the handover to start now (ACTIVE -> PENDING_VERIFICATION).</summary>
        public async Task<WillDto> RequestHandoverAsync(Guid ownerId, Guid willId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            if (will.Status != WillStatus.Active)
                throw ApiException.Conflict("Only an active will can start a handover");

            Trigger(will);
            return await SaveAsync(will, ct);
        }

        /// <summary>Owner proves they are alive: PENDING_VERIFICATION -> ACTIVE, DMS restarts.</summary>
        public async Task<WillDto> CancelHandoverAsync(Guid ownerId, Guid willId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            if (will.Status != WillStatus.PendingVerification)
                throw ApiException.Conflict("There is no open handover to cancel");

            will.Status = WillStatus.Active;
            Restart(will.Dms ?? throw ApiException.Conflict("DMS is not configured"), DateTime.UtcNow);
            return await SaveAsync(will, ct);
        }

        public async Task<int> EvaluateOverdueAsync(DateTime utcNow, CancellationToken ct)
        {
            var wills = await _db.Wills
                .Include(w => w.Dms)
                .Where(w => w.Status == WillStatus.Active
                            && w.Dms != null
                            && w.Dms.Status == DmsStatuses.Active
                            && w.Dms.NextDueAt != null
                            && w.Dms.NextDueAt < utcNow)
                .ToListAsync(ct);

            var triggered = 0;
            foreach (var will in wills)
            {
                var dms = will.Dms!;
                var interval = TimeSpan.FromDays(dms.IntervalDays);
                // every full interval past the due date is one missed check-in
                var missed = (int)Math.Floor((utcNow - dms.NextDueAt!.Value) / interval) + 1;
                dms.MissedCount += missed;
                dms.NextDueAt = dms.NextDueAt.Value.AddDays(missed * dms.IntervalDays);

                if (dms.MissedCount >= dms.MaxMissed)
                {
                    Trigger(will);
                    triggered++;
                }
                will.UpdatedAt = utcNow;
            }
            if (wills.Count > 0) await _db.SaveChangesAsync(ct);
            return triggered;
        }

        // ------------------------------------------------------------------

        private static void Restart(DmsConfiguration dms, DateTime now)
        {
            dms.Status = DmsStatuses.Active;
            dms.LastCheckInAt = now;
            dms.MissedCount = 0;
            dms.NextDueAt = now.AddDays(dms.IntervalDays);
        }

        private static void Trigger(Will will)
        {
            will.Status = WillStatus.PendingVerification;
            if (will.Dms != null) will.Dms.Status = DmsStatuses.Triggered;
        }

        private async Task<WillDto> SaveAsync(Will will, CancellationToken ct)
        {
            will.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return WillMapper.ToDto(will);
        }
    }
}
