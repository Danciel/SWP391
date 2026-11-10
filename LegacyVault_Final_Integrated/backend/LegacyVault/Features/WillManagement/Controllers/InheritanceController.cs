using System.Security.Cryptography;
using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers
{
    public sealed class VerifyAccessCodeRequest
    {
        public string? Code { get; set; }
    }

    // Beneficiary side of a handover (immediate or after death):
    //   list  -> open the will with the Security PIN -> type the access code
    //   that was emailed to you -> scan face to claim
    //   -> read the claimed asset's credentials (PIN again).
    // Nothing here changes who owns the Asset; the Owner's encryption key
    // binding stays intact and the Beneficiary only gets read access.
    [ApiController]
    [Authorize]
    [Route("api/inheritance")]
    public class InheritanceController : ControllerBase
    {
        private readonly LegacyVaultDbContext _db;
        private readonly ICurrentUserService _user;
        private readonly IIdpIntegrationService _idp;
        private readonly IAssetEncryptionService _encryption;
        private readonly IHandoverCodeService _codes;
        private readonly IHandoverService _handover;

        public InheritanceController(
            LegacyVaultDbContext db,
            ICurrentUserService user,
            IIdpIntegrationService idp,
            IAssetEncryptionService encryption,
            IHandoverCodeService codes,
            IHandoverService handover)
        {
            _db = db;
            _user = user;
            _idp = idp;
            _encryption = encryption;
            _codes = codes;
            _handover = handover;
        }

        // Summary only: no asset names, no secrets (PIN not needed).
        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var me = _user.GetRequiredAccountId();

            var rows = await _db.HandoverItems
                .AsNoTracking()
                .Where(i =>
                    i.AssetAssignment!.BeneficiaryDesignation!
                        .BeneficiaryAccountId == me &&
                    i.HandoverCase!.Status != "ABORTED")
                .Select(i => new
                {
                    i.Status,
                    i.ClaimedAt,
                    CaseId = i.HandoverCaseId,
                    CaseStatus = i.HandoverCase!.Status,
                    CaseType = i.HandoverCase.Type,
                    StartedAt = i.HandoverCase.StartedAt,
                    WillId = i.HandoverCase.WillId,
                    WillTitle = i.HandoverCase.Will!.Title,
                    OwnerName = i.HandoverCase.Will.OwnerAccount != null
                        ? i.HandoverCase.Will.OwnerAccount.FullName
                        : string.Empty
                })
                .ToListAsync();

            var result = rows
                .GroupBy(r => r.CaseId)
                .Select(g =>
                {
                    var first = g.First();
                    var total = g.Count();
                    var claimed = g.Count(x => x.ClaimedAt != null);
                    var ready = g.Count(x =>
                        x.Status == "DELIVERED" && x.ClaimedAt == null);

                    var claimStatus =
                        claimed == total ? "CLAIMED"
                        : ready > 0 ? "READY_TO_CLAIM"
                        : "WAITING_FOR_EXECUTOR";

                    return new
                    {
                        willId = first.WillId,
                        willTitle = first.WillTitle,
                        ownerName = first.OwnerName,
                        handoverType = first.CaseType,
                        handoverStatus = first.CaseStatus,
                        startedAt = first.StartedAt,
                        totalItems = total,
                        readyItems = ready,
                        claimedItems = claimed,
                        claimStatus
                    };
                })
                .OrderByDescending(x => x.startedAt)
                .ToList();

            return Ok(result);
        }

        // Opens the will for the Beneficiary. Requires the Security PIN
        // (the PIN filter reads the route value "id" = Will ID).
        [HttpGet("{id:guid}")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public async Task<IActionResult> Detail(Guid id)
        {
            var me = _user.GetRequiredAccountId();

            var caseRow = await _db.HandoverCases
                .AsNoTracking()
                .Where(x => x.WillId == id && x.Status != "ABORTED")
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Type,
                    x.Status,
                    x.StartedAt,
                    WillTitle = x.Will!.Title,
                    WillDescription = x.Will.Description,
                    OwnerName = x.Will.OwnerAccount != null
                        ? x.Will.OwnerAccount.FullName
                        : string.Empty
                })
                .FirstOrDefaultAsync();

            if (caseRow == null)
                return NotFound(new { message = "Inherited will not found." });

            var items = await _db.HandoverItems
                .AsNoTracking()
                .Where(i =>
                    i.HandoverCaseId == caseRow.Id &&
                    i.AssetAssignment!.BeneficiaryDesignation!
                        .BeneficiaryAccountId == me)
                .Select(i => new
                {
                    itemId = i.Id,
                    assetName = i.AssetAssignment!.WillAsset!.Asset!.Name,
                    assetType = i.AssetAssignment.WillAsset.Asset.AssetType,
                    provider = i.AssetAssignment.WillAsset.Asset.Provider,
                    allocationPercentage = i.AssetAssignment.AllocationPercentage,
                    relationship = i.AssetAssignment.BeneficiaryDesignation!
                        .Relationship,
                    status = i.Status,
                    deliveredAt = i.DeliveredAt,
                    claimedAt = i.ClaimedAt,
                    canClaim = i.Status == "DELIVERED" && i.ClaimedAt == null
                })
                .ToListAsync();

            if (items.Count == 0)
                return NotFound(new { message = "Inherited will not found." });

            // Per-recipient access code: confirmed before any asset is received.
            var designationId = await _db.HandoverItems
                .AsNoTracking()
                .Where(i =>
                    i.HandoverCaseId == caseRow.Id &&
                    i.AssetAssignment!.BeneficiaryDesignation!
                        .BeneficiaryAccountId == me)
                .Select(i => i.AssetAssignment!.BeneficiaryDesignationId)
                .FirstAsync();

            var accessCode = await _codes.GetStatusAsync(caseRow.Id, designationId);

            // A handover that started before access codes existed: send it now.
            if (accessCode.Status == "NOT_ISSUED" &&
                items.Any(i => i.status == "DELIVERED"))
            {
                await _codes.IssueForCaseAsync(caseRow.Id);
                accessCode = await _codes.GetStatusAsync(caseRow.Id, designationId);

                // Still nothing after a delivery: tell the person the truth instead of
                // letting the page say the Executor has not delivered.
                if (accessCode.Status == "NOT_ISSUED")
                    accessCode = accessCode with { Status = "UNAVAILABLE" };
            }

            var account = await _db.Accounts.AsNoTracking()
                .FirstAsync(x => x.Id == me);

            var accessCodeView = new
            {
                status = accessCode.Status,
                verified = accessCode.Verified,
                lockedUntil = accessCode.LockedUntil,
                verifiedUntil = accessCode.VerifiedUntil,
                issueCount = accessCode.IssueCount,
                maxIssues = accessCode.MaxIssues,
                nextResendAt = accessCode.NextResendAt,
                supportRequested = accessCode.SupportRequested,
                canAskForHelp = !accessCode.Verified &&
                    accessCode.MaxIssues > 0 &&
                    (accessCode.IssueCount >= accessCode.MaxIssues ||
                     accessCode.Status == "LOCKED")
            };

            // Until the access code is confirmed the person only gets an overview
            // ("you received a will"): no description, no asset names, no shares.
            if (!accessCode.Verified)
            {
                return Ok(new
                {
                    willId = id,
                    willTitle = caseRow.WillTitle,
                    ownerName = caseRow.OwnerName,
                    handoverType = caseRow.Type,
                    handoverStatus = caseRow.Status,
                    startedAt = caseRow.StartedAt,
                    identityStatus = account.IdentityStatus,
                    faceLinked = !string.IsNullOrWhiteSpace(account.ExternalIdentityId),
                    contentLocked = true,
                    totalItems = items.Count,
                    deliveredItems = items.Count(i => i.status == "DELIVERED"),
                    accessCode = accessCodeView,
                    items = Array.Empty<object>()
                });
            }

            return Ok(new
            {
                willId = id,
                willTitle = caseRow.WillTitle,
                description = caseRow.WillDescription,
                ownerName = caseRow.OwnerName,
                handoverType = caseRow.Type,
                handoverStatus = caseRow.Status,
                startedAt = caseRow.StartedAt,
                identityStatus = account.IdentityStatus,
                faceLinked = !string.IsNullOrWhiteSpace(account.ExternalIdentityId),
                contentLocked = false,
                accessCode = accessCodeView,
                items
            });
        }

        // Face scan = receipt. The photo is checked by the identity provider
        // against the face enrolled for this Beneficiary's linked identity.
        [HttpPost("items/{itemId:guid}/claim")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Claim(Guid itemId, IFormFile faceImage)
        {
            if (faceImage == null || faceImage.Length == 0)
                return BadRequest(new { message = "Face image is required." });

            var me = _user.GetRequiredAccountId();

            var item = await _db.HandoverItems
                .AsNoTracking()
                .Include(x => x.HandoverCase)
                .Include(x => x.AssetAssignment)
                    .ThenInclude(a => a!.BeneficiaryDesignation)
                .FirstOrDefaultAsync(x => x.Id == itemId);

            if (item?.AssetAssignment?.BeneficiaryDesignation
                    ?.BeneficiaryAccountId != me)
            {
                return NotFound(new { message = "Handover item not found." });
            }

            var handoverCase = item.HandoverCase!;

            if (handoverCase.Status == "PAUSED")
                return Conflict(new { message = "This handover is paused while a problem is being reviewed." });

            if (handoverCase.Status != "IN_PROGRESS" &&
                handoverCase.Status != "COMPLETED")
            {
                return Conflict(new { message = "This handover is not active." });
            }

            if (item.Status != "DELIVERED")
                return Conflict(new { message = "The Executor has not delivered this asset yet." });

            if (item.ClaimedAt != null)
                return Conflict(new { message = "You already received this asset." });

            // Order: Security PIN -> access code (emailed) -> face scan.
            var codeStatus = await _codes.GetStatusAsync(
                handoverCase.Id, item.AssetAssignment!.BeneficiaryDesignationId);

            if (!codeStatus.Verified)
            {
                if (codeStatus.Status == "NOT_ISSUED")
                    await _codes.IssueForCaseAsync(handoverCase.Id);

                return StatusCode(428, new
                {
                    code = "ACCESS_CODE_REQUIRED",
                    message = "Enter the access code that was emailed to you before receiving this asset."
                });
            }

            var account = await _db.Accounts.AsNoTracking()
                .FirstAsync(x => x.Id == me);

            if (!Guid.TryParse(account.ExternalIdentityId, out var idpUserId))
            {
                return BadRequest(new
                {
                    message = "Verify your identity (face enrollment) before receiving assets."
                });
            }

            await using var stream = faceImage.OpenReadStream();
            var faceCheck = await _idp.VerifyFaceAsync(
                idpUserId, stream, HttpContext.RequestAborted);

            var now = DateTimeOffset.UtcNow;

            if (!faceCheck.Success)
            {
                AddAudit(me, "INHERITANCE_CLAIM_FACE_FAILED", itemId,
                    new { willId = handoverCase.WillId });
                await _db.SaveChangesAsync();

                // Real reason (IdP unreachable, no enrolled face, mismatch, ...)
                return StatusCode(faceCheck.StatusCode, new
                {
                    message = faceCheck.Message
                });
            }

            // Atomic: an item can be claimed once.
            var rows = await _db.HandoverItems
                .Where(x => x.Id == itemId &&
                            x.Status == "DELIVERED" &&
                            x.ClaimedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.ClaimedAt, now)
                    .SetProperty(x => x.ClaimedByAccountId, me));

            if (rows == 0)
                return Conflict(new { message = "You already received this asset." });

            AddAudit(me, "INHERITANCE_ASSET_CLAIMED", itemId,
                new { willId = handoverCase.WillId });
            await _db.SaveChangesAsync();

            return Ok(new { itemId, status = "CLAIMED", claimedAt = now });
        }

        // The recipient types the access code from the email. The Security PIN
        // comes first (the PIN filter reads the route value "id" = Will ID).
        // 3 wrong codes lock it for 15 minutes.
        [HttpPost("{id:guid}/access-code/verify")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public async Task<IActionResult> VerifyAccessCode(
            Guid id, [FromBody] VerifyAccessCodeRequest request)
        {
            var me = _user.GetRequiredAccountId();
            var caseId = await FindCaseIdAsync(id);

            if (caseId == null)
                return NotFound(new { message = "Inherited will not found." });

            var result = await _codes.VerifyAsync(
                caseId.Value, me, request?.Code, HttpContext.RequestAborted);

            return ToCodeResponse(result);
        }

        // "Send the code again": a NEW code replaces the old one.
        [HttpPost("{id:guid}/access-code/resend")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public async Task<IActionResult> ResendAccessCode(Guid id)
        {
            var me = _user.GetRequiredAccountId();
            var caseId = await FindCaseIdAsync(id);

            if (caseId == null)
                return NotFound(new { message = "Inherited will not found." });

            var result = await _codes.ResendAsync(
                caseId.Value, me, HttpContext.RequestAborted);

            return ToCodeResponse(result);
        }

        // The recipient used all their new codes (or is locked): the Executor is
        // emailed and the request is kept in the audit trail for the support team.
        [HttpPost("{id:guid}/access-code/support")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public async Task<IActionResult> RequestAccessCodeHelp(Guid id)
        {
            var me = _user.GetRequiredAccountId();
            var caseId = await FindCaseIdAsync(id);

            if (caseId == null)
                return NotFound(new { message = "Inherited will not found." });

            var result = await _codes.RequestSupportAsync(
                caseId.Value, me, HttpContext.RequestAborted);

            return ToCodeResponse(result);
        }

        private Task<Guid?> FindCaseIdAsync(Guid willId) =>
            _db.HandoverCases
                .AsNoTracking()
                .Where(x => x.WillId == willId && x.Status != "ABORTED")
                .OrderByDescending(x => x.StartedAt)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

        private IActionResult ToCodeResponse(AccessCodeResult result) =>
            result.Success
                ? Ok(new { message = result.Message })
                : StatusCode(result.StatusCode, new
                {
                    message = result.Message,
                    remainingAttempts = result.RemainingAttempts,
                    lockedUntil = result.LockedUntil
                });

        // C3: the minutes as this beneficiary may see them (own lines only).
        [HttpGet("{id:guid}/minutes")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public Task<IActionResult> Minutes(Guid id)
            => RunMinutes(() => _handover.GetMinutesAsync(
                id, _user.GetRequiredAccountId(), false));

        // C3: the beneficiary signs after receiving ALL of their assets.
        [HttpPost("{id:guid}/minutes/sign")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public Task<IActionResult> SignMinutes(Guid id)
            => RunMinutes(() => _handover.SignAsBeneficiaryAsync(
                id,
                _user.GetRequiredAccountId(),
                HttpContext.Connection.RemoteIpAddress?.ToString()));

        private async Task<IActionResult> RunMinutes<T>(Func<Task<T>> action)
        {
            try { return Ok(await action()); }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (Services.WillConflictException ex)
            { return Conflict(new { message = ex.Message }); }
            catch (InvalidOperationException ex)
            { return BadRequest(new { message = ex.Message }); }
        }

        // Credentials of a CLAIMED asset. Needs the Security PIN again.
        // Route value "id" = Will ID (used by the PIN filter).
        [HttpGet("{id:guid}/items/{itemId:guid}/secret")]
        [RequireOwnerPin(OwnerPinScopes.InheritanceView)]
        public async Task<IActionResult> Secret(Guid id, Guid itemId)
        {
            var me = _user.GetRequiredAccountId();

            var item = await _db.HandoverItems
                .AsNoTracking()
                .Include(x => x.HandoverCase)
                .Include(x => x.AssetAssignment)
                    .ThenInclude(a => a!.BeneficiaryDesignation)
                .Include(x => x.AssetAssignment)
                    .ThenInclude(a => a!.WillAsset)
                .FirstOrDefaultAsync(x => x.Id == itemId);

            if (item == null ||
                item.HandoverCase!.WillId != id ||
                item.AssetAssignment?.BeneficiaryDesignation
                    ?.BeneficiaryAccountId != me)
            {
                return NotFound(new { message = "Handover item not found." });
            }

            if (item.ClaimedAt == null)
                return StatusCode(403, new { message = "Scan your face to receive this asset first." });

            if (item.HandoverCase.Status == "PAUSED")
                return Conflict(new { message = "This handover is paused while a problem is being reviewed." });

            var assetId = item.AssetAssignment!.WillAsset!.AssetId;

            var asset = await _db.Assets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == assetId);
            var secret = await _db.AssetSecrets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AssetId == assetId);

            if (asset == null || secret == null)
            {
                return NotFound(new
                {
                    message = "This asset has no stored credentials (for example a document)."
                });
            }

            Dictionary<string, string> data;
            try
            {
                // Encrypted with the Owner's id as context; decrypt the same way.
                var json = _encryption.Decrypt(asset.Id, asset.OwnerAccountId, secret);
                data = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch (Exception ex) when (
                ex is CryptographicException or InvalidOperationException or JsonException)
            {
                return BadRequest(new { message = "The asset credentials could not be opened." });
            }

            AddAudit(me, "INHERITED_SECRET_REVEALED", itemId, new { willId = id, assetId });
            await _db.SaveChangesAsync();

            return Ok(new { assetId, secretData = data });
        }

        private void AddAudit(Guid actor, string action, Guid entityId, object details)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = actor,
                Action = action,
                EntityType = "HandoverItem",
                EntityId = entityId,
                Details = JsonSerializer.Serialize(details),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
