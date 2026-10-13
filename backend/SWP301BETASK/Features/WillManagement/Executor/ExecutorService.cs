using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Features.WillManagement.Services;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Executor
{
    public class ExecutorService : IExecutorService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly IWillSubmissionService _submission;

        public ExecutorService(LegacyVaultDbContext db, IWillSubmissionService submission)
        {
            _db = db;
            _submission = submission;
        }

        public async Task<List<ExecutorDto>> ListAsync(CancellationToken ct) =>
            (await _db.ExecutorProfiles.AsNoTracking().Where(e => e.Available).OrderBy(e => e.Name).ToListAsync(ct))
            .Select(WillMapper.ToDto).ToList();

        public async Task<WillDto> SetAsync(Guid ownerId, Guid willId, SetExecutorRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.ExecutorEditable, "Executor cannot be changed in the current will status");

            var profile = await _db.ExecutorProfiles.FirstOrDefaultAsync(e => e.Id == req.ExecutorId, ct)
                          ?? throw ApiException.NotFound("Executor not found");
            if (!profile.Available) throw ApiException.Conflict("This executor is not available");

            if (will.Executor == null)
            {
                will.Executor = new ExecutorAssignment { Id = Guid.NewGuid(), WillId = will.Id };
                _db.ExecutorAssignments.Add(will.Executor);
            }
            will.Executor.ExecutorProfileId = profile.Id;
            will.Executor.Profile = profile;
            will.Executor.Fee = profile.Fee;

            if (will.Status == WillStatus.NeedsExecutor)
            {
                // previous executor declined -> hire the new one right away (same as the FE flow)
                await _submission.RequireSubmittableAsync(will, ct);
                _submission.SendHireRequest(will);
                will.Status = WillStatus.PendingExecutorAcceptance;
            }
            else
            {
                will.Executor.Status = ExecutorAssignmentStatuses.Selected;
                will.Executor.PaymentStatus = PaymentStatuses.None;
                will.Executor.RequestedAt = null;
                will.Executor.DeadlineAt = null;
            }

            will.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return WillMapper.ToDto(will);
        }

        public async Task ResolveHiringAsync(Guid willId, bool accepted, CancellationToken ct)
        {
            var will = await _db.Wills.Include(w => w.Executor).Include(w => w.Dms)
                           .FirstOrDefaultAsync(w => w.Id == willId, ct)
                       ?? throw ApiException.NotFound("Will not found");
            if (will.Status != WillStatus.PendingExecutorAcceptance || will.Executor == null)
                throw ApiException.Conflict("Will is not waiting for an executor response");

            var now = DateTime.UtcNow;
            will.Executor.RespondedAt = now;
            will.Executor.Status = accepted ? ExecutorAssignmentStatuses.Accepted : ExecutorAssignmentStatuses.Declined;
            will.Executor.PaymentStatus = accepted ? PaymentStatuses.Paid : PaymentStatuses.Released;
            will.Status = accepted ? WillStatus.Active : WillStatus.NeedsExecutor;

            if (accepted && will.Dms != null)
            {
                will.Dms.Status = DmsStatuses.Active;
                will.Dms.LastCheckInAt = now;
                will.Dms.MissedCount = 0;
                will.Dms.NextDueAt = now.AddDays(will.Dms.IntervalDays);
            }
            will.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
        }
    }
}
