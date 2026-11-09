using LegacyVault.Data;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

// W09: relationship views limited to what the caller is allowed to see.
//  - Owner: own wills, their beneficiaries and executor.
//  - Beneficiary: only wills whose handover has started, and ONLY the
//    caller's own relationship and own assets (never the other people).
//  - Executor: only wills assigned to the caller, without beneficiary data.
[ApiController]
[Authorize]
[Route("api/relationships")]
public class RelationshipsController : ControllerBase
{
    private static readonly string[] BeneficiaryVisibleStatuses =
        { "HANDOVER_IN_PROGRESS", "HANDED_OVER" };

    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _user;

    public RelationshipsController(LegacyVaultDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var me = _user.GetRequiredAccountId();

        var asOwner = await _db.Wills.AsNoTracking()
            .Where(w => w.OwnerAccountId == me && w.Status != "CANCELLED")
            .OrderByDescending(w => w.UpdatedAt)
            .Select(w => new
            {
                willId = w.Id,
                title = w.Title,
                status = w.Status,
                beneficiaries = w.Beneficiaries.Select(b => new
                {
                    id = b.Id,
                    fullName = b.FullName,
                    relationship = b.Relationship,
                    status = b.Status
                }).ToList(),
                executor = w.ExecutorAssignment == null ? null : new
                {
                    accountId = w.ExecutorAssignment.ExecutorAccountId,
                    fullName = w.ExecutorAssignment.ExecutorAccount != null
                        ? w.ExecutorAssignment.ExecutorAccount.FullName : string.Empty,
                    status = w.ExecutorAssignment.Status
                }
            }).ToListAsync();

        var asBeneficiary = await _db.BeneficiaryDesignations.AsNoTracking()
            .Where(b => b.BeneficiaryAccountId == me &&
                        b.Will != null &&
                        BeneficiaryVisibleStatuses.Contains(b.Will.Status))
            .Select(b => new
            {
                willId = b.WillId,
                willTitle = b.Will!.Title,
                willStatus = b.Will.Status,
                ownerName = b.Will.OwnerAccount != null ? b.Will.OwnerAccount.FullName : string.Empty,
                myRelationship = b.Relationship,
                myAssets = b.AssetAssignments.Select(a => new
                {
                    assignmentId = a.Id,
                    assetName = a.WillAsset!.Asset != null ? a.WillAsset.Asset.Name : string.Empty,
                    allocationPercentage = a.AllocationPercentage
                }).ToList()
            }).ToListAsync();

        var asExecutor = await _db.ExecutorAssignments.AsNoTracking()
            .Where(x => x.ExecutorAccountId == me &&
                        (x.Status == "PENDING" || x.Status == "ACCEPTED") &&
                        x.Will != null)
            .Select(x => new
            {
                willId = x.WillId,
                willTitle = x.Will!.Title,
                willStatus = x.Will.Status,
                ownerName = x.Will.OwnerAccount != null ? x.Will.OwnerAccount.FullName : string.Empty,
                assignmentStatus = x.Status,
                scope = x.Scope,
                serviceFee = x.ServiceFeeSnapshot,
                currency = x.CurrencySnapshot
            }).ToListAsync();

        return Ok(new { asOwner, asBeneficiary, asExecutor });
    }

    // Owner only: where one of MY assets is used and who receives it.
    [HttpGet("assets/{assetId:guid}")]
    public async Task<IActionResult> Asset(Guid assetId)
    {
        var me = _user.GetRequiredAccountId();

        var owned = await _db.Assets.AsNoTracking()
            .AnyAsync(a => a.Id == assetId && a.OwnerAccountId == me);
        if (!owned) return NotFound(new { message = "Asset not found." });

        var wills = await _db.WillAssets.AsNoTracking()
            .Where(wa => wa.AssetId == assetId &&
                         wa.Will != null &&
                         wa.Will.OwnerAccountId == me &&
                         wa.Will.Status != "CANCELLED")
            .Select(wa => new
            {
                willId = wa.WillId,
                willTitle = wa.Will!.Title,
                willStatus = wa.Will.Status,
                beneficiaries = wa.Assignments.Select(a => new
                {
                    assignmentId = a.Id,
                    fullName = a.BeneficiaryDesignation != null ? a.BeneficiaryDesignation.FullName : string.Empty,
                    relationship = a.BeneficiaryDesignation != null ? a.BeneficiaryDesignation.Relationship : null,
                    allocationPercentage = a.AllocationPercentage
                }).ToList()
            }).ToListAsync();

        return Ok(new { assetId, wills });
    }
}
