using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Dms;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Services
{
    public class WillSubmissionService : IWillSubmissionService
    {
        public const int ExecutorResponseDays = 7;

        private readonly LegacyVaultDbContext _db;
        private readonly IAccountLookup _accounts;
        private readonly IAssetLookup _assets;

        public WillSubmissionService(LegacyVaultDbContext db, IAccountLookup accounts, IAssetLookup assets)
        {
            _db = db;
            _accounts = accounts;
            _assets = assets;
        }

        public async Task<WillReviewDto> ReviewAsync(Guid ownerId, Guid willId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct, tracking: false);
            var verified = await _accounts.IsIdentityVerifiedAsync(ownerId, ct);
            var checks = await BuildChecksAsync(will, verified, ct);
            var errors = checks.Where(c => !c.Passed).Select(c => c.Message).ToList();
            return new WillReviewDto(WillMapper.ToDto(will), verified, checks,
                will.Status == WillStatus.Draft && errors.Count == 0, errors);
        }

        public async Task<WillDto> SubmitAsync(Guid ownerId, Guid willId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            if (will.Status != WillStatus.Draft)
                throw ApiException.Conflict("Only a draft will can be submitted");

            await RequireSubmittableAsync(will, ct);

            SendHireRequest(will);
            will.Status = WillStatus.PendingExecutorAcceptance;
            will.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return WillMapper.ToDto(will);
        }

        public async Task RequireSubmittableAsync(Will will, CancellationToken ct)
        {
            var verified = await _accounts.IsIdentityVerifiedAsync(will.OwnerId, ct);
            var errors = (await BuildChecksAsync(will, verified, ct)).Where(c => !c.Passed).Select(c => c.Message).ToList();
            if (errors.Count > 0) throw ApiException.BadRequest("Will is not ready to submit", errors);
        }

        public void SendHireRequest(Will will)
        {
            var now = DateTime.UtcNow;
            var ex = will.Executor ?? throw ApiException.BadRequest("Select an executor first");
            ex.Status = ExecutorAssignmentStatuses.Pending;
            ex.Fee = ex.Profile.Fee;
            ex.PaymentStatus = PaymentStatuses.Held;   // fee is held until the executor accepts
            ex.RequestedAt = now;
            ex.DeadlineAt = now.AddDays(ExecutorResponseDays);
            ex.RespondedAt = null;
        }

        private async Task<List<ReviewCheckDto>> BuildChecksAsync(Will will, bool identityVerified, CancellationToken ct)
        {
            var checks = new List<ReviewCheckDto>();
            void Add(string key, bool ok, string msg) => checks.Add(new ReviewCheckDto(key, ok, msg));

            Add("status", will.Status is WillStatus.Draft or WillStatus.NeedsExecutor,
                "Will must be a draft (or waiting for a new executor)");
            Add("name", !string.IsNullOrWhiteSpace(will.Name), "Will name is required");
            Add("assets", will.Assignments.Count > 0, "Select at least one asset");

            var unassigned = will.Assignments.Count(a => a.BeneficiaryDesignationId == null);
            Add("beneficiaries", will.Assignments.Count > 0 && unassigned == 0,
                unassigned == 0 ? "All assets have a beneficiary" : $"{unassigned} asset(s) still have no beneficiary");

            // assets must still belong to the owner (AssetManagement may have deleted/moved one)
            var owned = await _assets.GetOwnedAssetIdsAsync(will.OwnerId, will.Assignments.Select(a => a.AssetId), ct);
            Add("assetsOwned", will.Assignments.All(a => owned.Contains(a.AssetId)),
                "Some selected assets no longer exist or are not yours");

            Add("noSelfBeneficiary", will.Beneficiaries.All(b => b.BeneficiaryAccountId != will.OwnerId),
                "You cannot be your own beneficiary");

            var exec = will.Executor?.Profile;
            Add("executor", exec != null && exec.Available, "Select an available executor");

            Add("dms", DmsRules.IsValid(will.Dms), "Dead Man's Switch configuration is invalid");
            Add("identity", identityVerified, "Identity verification is required");
            return checks;
        }
    }
}
