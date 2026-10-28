using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AssetManagement.Controllers;

// Supplemental route preserving the original asset service and routes.
[ApiController, Authorize, Route("api/asset-maintenance")]
public sealed class AssetMaintenanceController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAssetService _assetService;
    public AssetMaintenanceController(LegacyVaultDbContext db, ICurrentUserService currentUser, IAssetService assetService)
    { _db = db; _currentUser = currentUser; _assetService = assetService; }

    [HttpDelete("{id:guid}")]
    [RequireOwnerPin(OwnerPinScopes.AssetDelete)]
    public async Task<IActionResult> DeleteUnused(Guid id)
    {
        var owner = _currentUser.GetRequiredAccountId();
        if (!await _db.Assets.AsNoTracking().AnyAsync(a => a.Id == id && a.OwnerAccountId == owner))
            return NotFound(new { message = "Asset not found." });
        if (await _db.WillAssets.AsNoTracking().AnyAsync(wa => wa.AssetId == id))
            return Conflict(new { message = "This asset is used in a Will. Remove its Will references before deleting." });
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var asset = await _db.Assets.FirstAsync(a => a.Id == id && a.OwnerAccountId == owner);
            // Explicitly remove verification and secret rows for databases without cascade DDL.
            var verifications = await _db.AssetVerifications.Where(v => v.AssetId == id).ToListAsync();
            _db.AssetVerifications.RemoveRange(verifications);
            var secret = await _db.AssetSecrets.FirstOrDefaultAsync(v => v.AssetId == id);
            if (secret != null) _db.AssetSecrets.Remove(secret);
            _db.Assets.Remove(asset);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Asset has related records and cannot be deleted." });
        }
    }
}
