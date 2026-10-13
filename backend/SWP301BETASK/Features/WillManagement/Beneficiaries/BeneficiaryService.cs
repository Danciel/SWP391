using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Features.WillManagement.Services;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Beneficiaries
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly IAccountLookup _accounts;

        public BeneficiaryService(LegacyVaultDbContext db, IAccountLookup accounts)
        {
            _db = db;
            _accounts = accounts;
        }

        public async Task<WillDto> AddAsync(Guid ownerId, Guid willId, BeneficiaryRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.BeneficiaryEditable, "Beneficiaries cannot be changed in the current will status");
            await RequireIdentityVerifiedAsync(ownerId, ct);

            var data = await ResolveAsync(ownerId, req, ct);

            // de-duplicate inside the same will (same account, or same email)
            var existing = will.Beneficiaries.FirstOrDefault(b =>
                (data.AccountId != null && b.BeneficiaryAccountId == data.AccountId) ||
                string.Equals(b.Email, data.Email, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                Apply(existing, data);
            }
            else
            {
                var b = new BeneficiaryDesignation { Id = Guid.NewGuid(), WillId = will.Id, CreatedAt = DateTime.UtcNow };
                Apply(b, data);
                will.Beneficiaries.Add(b);
            }
            return await SaveAsync(will, ct);
        }

        public async Task<WillDto> UpdateAsync(Guid ownerId, Guid willId, Guid beneficiaryId, BeneficiaryRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.BeneficiaryEditable, "Beneficiaries cannot be changed in the current will status");
            var b = will.Beneficiaries.FirstOrDefault(x => x.Id == beneficiaryId)
                    ?? throw ApiException.NotFound("Beneficiary not found in this will");

            Apply(b, await ResolveAsync(ownerId, req, ct));
            return await SaveAsync(will, ct);
        }

        public async Task<WillDto> RemoveAsync(Guid ownerId, Guid willId, Guid beneficiaryId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.BeneficiaryEditable, "Beneficiaries cannot be changed in the current will status");
            var b = will.Beneficiaries.FirstOrDefault(x => x.Id == beneficiaryId)
                    ?? throw ApiException.NotFound("Beneficiary not found in this will");

            foreach (var a in will.Assignments.Where(a => a.BeneficiaryDesignationId == b.Id))
                Unassign(a);

            will.Beneficiaries.Remove(b);
            _db.BeneficiaryDesignations.Remove(b);
            return await SaveAsync(will, ct);
        }

        public async Task<WillDto> AssignAsync(Guid ownerId, Guid willId, AssignAssetRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.BeneficiaryEditable, "Assignments cannot be changed in the current will status");

            var assignment = will.Assignments.FirstOrDefault(a => a.AssetId == req.AssetId)
                             ?? throw ApiException.BadRequest("This asset is not part of the will. Select it first (PUT /assets).");

            if (req.BeneficiaryId == null)
            {
                Unassign(assignment);
            }
            else
            {
                await RequireIdentityVerifiedAsync(ownerId, ct);
                var b = will.Beneficiaries.FirstOrDefault(x => x.Id == req.BeneficiaryId)
                        ?? throw ApiException.NotFound("Beneficiary not found in this will");

                assignment.BeneficiaryDesignationId = b.Id;
                assignment.Beneficiary = b;
                assignment.AssignedAt = DateTime.UtcNow;
                assignment.RevokedAt = null;
                assignment.AssignmentStatus = AssignmentStatuses.Active;
            }
            return await SaveAsync(will, ct);
        }

        // ------------------------------------------------------------------

        private record BeneficiaryData(Guid? AccountId, string FullName, string Email, string Phone, string? IdentityNumber, string Relationship);

        private async Task<BeneficiaryData> ResolveAsync(Guid ownerId, BeneficiaryRequest req, CancellationToken ct)
        {
            var relationship = string.IsNullOrWhiteSpace(req.Relationship) ? "Other" : req.Relationship.Trim();

            if (req.AccountId != null)
            {
                if (req.AccountId == ownerId) throw ApiException.BadRequest("You cannot be your own beneficiary");
                var acc = await _accounts.GetAsync(req.AccountId.Value, ct)
                          ?? throw ApiException.NotFound("Beneficiary account not found");
                return new BeneficiaryData(acc.Id, acc.FullName, acc.Email, acc.Phone,
                    string.IsNullOrWhiteSpace(req.IdentityNumber) ? null : req.IdentityNumber.Trim(), relationship);
            }

            if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
                throw ApiException.BadRequest("fullName and email are required for a beneficiary without an account");

            return new BeneficiaryData(null, req.FullName.Trim(), req.Email.Trim().ToLowerInvariant(),
                req.Phone?.Trim() ?? string.Empty,
                string.IsNullOrWhiteSpace(req.IdentityNumber) ? null : req.IdentityNumber.Trim(), relationship);
        }

        private static void Apply(BeneficiaryDesignation b, BeneficiaryData d)
        {
            b.BeneficiaryAccountId = d.AccountId;
            b.FullName = d.FullName;
            b.Email = d.Email;
            b.Phone = d.Phone;
            b.IdentityNumber = d.IdentityNumber;
            b.Relationship = d.Relationship;
        }

        private static void Unassign(AssetAssignment a)
        {
            a.BeneficiaryDesignationId = null;
            a.Beneficiary = null;
            a.RevokedAt = DateTime.UtcNow;
            a.AssignmentStatus = AssignmentStatuses.Unassigned;
        }

        private async Task RequireIdentityVerifiedAsync(Guid ownerId, CancellationToken ct)
        {
            if (!await _accounts.IsIdentityVerifiedAsync(ownerId, ct))
                throw ApiException.Forbidden("You must verify your identity before assigning beneficiaries");
        }

        private async Task<WillDto> SaveAsync(Will will, CancellationToken ct)
        {
            will.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return WillMapper.ToDto(will);
        }
    }
}
