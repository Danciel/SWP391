using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Services
{
    /// <summary>Shared loading / guard helpers used by every WillManagement service.</summary>
    internal static class WillQueries
    {
        public static IQueryable<Will> WithDetails(this IQueryable<Will> query) =>
            query.Include(w => w.Beneficiaries)
                 .Include(w => w.Assignments).ThenInclude(a => a.Beneficiary)
                 .Include(w => w.Executor).ThenInclude(e => e!.Profile)
                 .Include(w => w.Dms)
                 .AsSplitQuery();

        /// <summary>Loads a will with all details. Someone else's will is reported as 404 (don't leak existence).</summary>
        public static async Task<Will> LoadOwnedAsync(this LegacyVaultDbContext db, Guid ownerId, Guid willId,
            CancellationToken ct, bool tracking = true)
        {
            var query = db.Wills.WithDetails();
            if (!tracking) query = query.AsNoTracking();

            var will = await query.FirstOrDefaultAsync(w => w.Id == willId, ct);
            if (will == null || will.OwnerId != ownerId)
                throw ApiException.NotFound("Will not found");
            return will;
        }

        public static void RequireStatus(this Will will, string[] allowed, string message)
        {
            if (!allowed.Contains(will.Status)) throw ApiException.Conflict(message);
        }
    }

    /// <summary>Which edits are allowed in which will status.</summary>
    internal static class WillRules
    {
        public static readonly string[] DraftOnly = { WillStatus.Draft };
        public static readonly string[] BeneficiaryEditable = { WillStatus.Draft, WillStatus.Active, WillStatus.NeedsExecutor };
        public static readonly string[] ExecutorEditable = { WillStatus.Draft, WillStatus.NeedsExecutor };
        public static readonly string[] DmsEditable = { WillStatus.Draft, WillStatus.Active, WillStatus.NeedsExecutor };
    }
}
