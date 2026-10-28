using System.Security.Cryptography;
using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers
{
    // Beneficiary side of a handover (immediate or after death):
    //   list  -> open the will with the Security PIN -> scan face to claim
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

        public InheritanceController(
            LegacyVaultDbContext db,
            ICurrentUserService user,
            IIdpIntegrationService idp,
            IAssetEncryptionService encryption)
        {
            _db = db;
            _user = user;
            _idp = idp;
            _encryption = encryption;
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

            var account = await _db.Accounts.AsNoTracking()
                .FirstAsync(x => x.Id == me);

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
