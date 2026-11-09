using LegacyVault.Data;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

// Supplementary endpoint: only an owner may remove a will declined by its executor.
[ApiController, Authorize, Route("api/will-maintenance")]
public sealed class WillMaintenanceController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _currentUser;
    public WillMaintenanceController(LegacyVaultDbContext db, ICurrentUserService currentUser)
    { _db = db; _currentUser = currentUser; }

    [HttpDelete("{id:guid}/declined")]
    public async Task<IActionResult> DeleteDeclined(Guid id)
    {
        var owner = _currentUser.GetRequiredAccountId();
        var will = await _db.Wills
            .Include(w => w.ExecutorAssignment)
            .FirstOrDefaultAsync(w => w.Id == id && w.OwnerAccountId == owner);
        if (will == null) return NotFound(new { message = "Will not found." });
        if (will.Status != "PENDING_EXECUTOR" || will.ExecutorAssignment?.Status != "DECLINED")
            return BadRequest(new { message = "Only a Will declined by its Executor can be deleted through this endpoint." });
        _db.Wills.Remove(will);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
