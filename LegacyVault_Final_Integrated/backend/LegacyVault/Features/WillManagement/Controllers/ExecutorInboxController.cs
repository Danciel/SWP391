using LegacyVault.Data;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

// Additional executor inbox endpoints; existing will services remain untouched.
[ApiController, Authorize, Route("api/executor-inbox")]
public class ExecutorInboxController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly LegacyVault.Features.WillManagement.Contracts.IWalletService _wallet;
    public ExecutorInboxController(LegacyVaultDbContext db, ICurrentUserService user, LegacyVault.Features.WillManagement.Contracts.IWalletService wallet) { _db=db; _user=user; _wallet=wallet; }

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var me=_user.GetRequiredAccountId();
        var assignments=await _db.ExecutorAssignments.AsNoTracking()
            .Where(x=>x.ExecutorAccountId==me && x.Will!=null && x.Will.Status!="DRAFT")
            .OrderByDescending(x=>x.AssignedAt)
            .Select(x=>new {
                id=x.Id, willId=x.WillId, willTitle=x.Will!.Title,
                ownerName=x.Will.OwnerAccount!=null?x.Will.OwnerAccount.FullName:"",
                ownerEmail=x.Will.OwnerAccount!=null?x.Will.OwnerAccount.Email:null,
                ownerPhone=x.Will.OwnerAccount!=null?x.Will.OwnerAccount.Phone:null,
                assetsCount=x.Will.WillAssets.Count,
                status=x.Status, sentAt=x.Will.SubmittedAt,
                assets=x.Will.WillAssets.Select(wa=>new {
                    willAssetId=wa.Id, assetName=wa.Asset!=null?wa.Asset.Name:"Asset",
                    assetType=wa.Asset!=null?wa.Asset.AssetType:"",
                    provider=wa.Asset!=null?wa.Asset.Provider:null,
                    beneficiaries=wa.Assignments.Select(asg=>new {
                        id=asg.BeneficiaryDesignationId,
                        fullName=asg.BeneficiaryDesignation!=null?asg.BeneficiaryDesignation.FullName:"",
                        relationship=asg.BeneficiaryDesignation!=null?asg.BeneficiaryDesignation.Relationship:null
                    }).ToList()
                }).ToList(),
                beneficiaries=x.Will.Beneficiaries.Select(b=>new {
                    id=b.Id, fullName=b.FullName, relationship=b.Relationship
                }).ToList()
            }).ToListAsync();
        return Ok(assignments);
    }
    // D06: wills whose DMS reached the missed threshold. Shown only to
    // the ACCEPTED executor of that will. No secrets, no death claim.
    [HttpGet("triggers")]
    public async Task<IActionResult> Triggers()
    {
        var me=_user.GetRequiredAccountId();
        var items=await _db.ExecutorAssignments.AsNoTracking()
            .Where(x=>x.ExecutorAccountId==me && x.Status=="ACCEPTED"
                && x.Will!=null && x.Will.DmsConfiguration!=null
                && x.Will.DmsConfiguration.Status=="THRESHOLD_REACHED")
            .Select(x=>new {
                assignmentId=x.Id, willId=x.WillId, willTitle=x.Will!.Title,
                ownerName=x.Will.OwnerAccount!=null?x.Will.OwnerAccount.FullName:"",
                missedCheckIns=x.Will.DmsConfiguration!.MissedCheckIns,
                maxMissedCheckIns=x.Will.DmsConfiguration.MaxMissedCheckIns,
                thresholdReachedAt=x.Will.DmsConfiguration.ThresholdReachedAt,
                lastCheckInAt=x.Will.DmsConfiguration.LastCheckInAt
            }).ToListAsync();
        return Ok(items);
    }

    [HttpPost("{assignmentId:guid}/respond")]
    public async Task<IActionResult> Respond(Guid assignmentId, [FromBody] ExecutorDecision request)
    {
        var me=_user.GetRequiredAccountId();
        var suspended = await _db.ExecutorProfiles.AnyAsync(x => x.AccountId == me && x.IsSuspended);
        if (suspended) return StatusCode(403, new { message = "This executor account is suspended and cannot respond to assignments." });
        var assignment=await _db.ExecutorAssignments.Include(x=>x.Will).FirstOrDefaultAsync(x=>x.Id==assignmentId && x.ExecutorAccountId==me);
        if (assignment==null) return NotFound(new {message="Executor assignment not found."});
        if (assignment.Will==null || assignment.Will.Status!="PENDING_EXECUTOR" || assignment.Status!="PENDING") return BadRequest(new {message="Assignment is not pending."});
        // Atomic guard: only one concurrent response can flip PENDING.
        var newStatus = request.Accept ? "ACCEPTED" : "DECLINED";
        var respondedAt = DateTimeOffset.UtcNow;
        var changed = await _db.ExecutorAssignments
            .Where(x => x.Id == assignmentId && x.Status == "PENDING")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, newStatus)
                .SetProperty(x => x.RespondedAt, respondedAt));
        if (changed == 0) return Conflict(new {message="Assignment was already answered."});
        if (!request.Accept) await _wallet.RefundFeeAsync(assignment.WillId);
        assignment.Status=newStatus;
        assignment.RespondedAt=respondedAt;
        // Executor acceptance activates the will; decline retains pending state for owner reassignment.
        if(request.Accept) { assignment.Will.Status="ACTIVE"; assignment.Will.ActivatedAt=DateTimeOffset.UtcNow; }
        assignment.Will.UpdatedAt=DateTimeOffset.UtcNow;
        _db.AuditLogs.Add(new LegacyVault.Models.AuditLog
        {
            Id = Guid.NewGuid(), ActorAccountId = me,
            Action = request.Accept ? "EXECUTOR_ACCEPTED" : "EXECUTOR_DECLINED",
            EntityType = "ExecutorAssignment", EntityId = assignment.Id,
            Details = System.Text.Json.JsonSerializer.Serialize(new { assignment.WillId, willStatus = assignment.Will.Status }),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new {assignment.Id,assignment.WillId,assignment.Status,willStatus=assignment.Will.Status});
    }
    public class ExecutorDecision { public bool Accept {get;set;} }
}
