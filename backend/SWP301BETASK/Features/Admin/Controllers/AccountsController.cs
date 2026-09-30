using SWP301BETASK.Data;
using SWP301BETASK.Models;
using SWP301BETASK.Features.Admin.DTOs;
using SWP301BETASK.Features.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace SWP301BETASK.Features.Admin.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
[EnableRateLimiting("auth")]
public class AccountsController(LegacyVaultDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<AccountResponse>> GetMe(CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        return account is null ? Unauthorized() : Ok(account.ToResponse());
    }

    /// <summary>Partial update: omitted/null fields are left unchanged.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<AccountResponse>> UpdateMe(UpdateProfileRequest req, CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        if (account is null) return Unauthorized();

        if (req.FullName is not null)
            account.FullName = req.FullName.Trim();

        if (req.Email is not null)
        {
            var email = Normalize.Email(req.Email);
            if (email != account.Email)
            {
                if (await db.Accounts.AnyAsync(a => a.Email == email && a.Id != account.Id, ct))
                    return Problem(detail: "Email is already in use.", statusCode: StatusCodes.Status409Conflict);
                account.Email = email;
                account.EmailVerified = false;
            }
        }

        if (req.Phone is not null)
        {
            var phone = Normalize.Phone(req.Phone);
            if (phone != account.Phone)
            {
                if (phone != null && await db.Accounts.AnyAsync(a => a.Phone == phone && a.Id != account.Id, ct))
                    return Problem(detail: "Phone is already in use.", statusCode: StatusCodes.Status409Conflict);
                account.Phone = phone;
                account.PhoneVerified = false;
            }
        }

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            return Problem(detail: "Email or phone is already in use.", statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(account.ToResponse());
    }

    /// <summary>
    /// Exact-match lookup by ?email= or ?phone= (exactly one). Admins get the full record;
    /// everyone else gets a minimal summary (no contact details, no enumeration by prefix).
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? email, [FromQuery] string? phone, CancellationToken ct)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPhone = !string.IsNullOrWhiteSpace(phone);
        if (hasEmail == hasPhone)
            return Problem(detail: "Provide exactly one of 'email' or 'phone'.", statusCode: StatusCodes.Status400BadRequest);

        var query = db.Accounts.AsNoTracking().Where(a => a.IsActive);
        Account? found;

        if (hasEmail)
        {
            var e = Normalize.Email(email!);
            found = await query.FirstOrDefaultAsync(a => a.Email == e, ct);
        }
        else
        {
            var p = Normalize.Phone(phone);
            found = await query.FirstOrDefaultAsync(a => a.Phone == p, ct);
        }

        if (found is null) return NotFound();

        return currentUser.IsInRole(UserRole.ADMIN)
            ? Ok(found.ToResponse())
            : Ok(found.ToSummary());
    }

    /// <summary>Admin-only role management. Affects new tokens; existing JWTs keep the old role until they expire.</summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = nameof(UserRole.ADMIN))]
    public async Task<ActionResult<AccountResponse>> ChangeRole(Guid id, ChangeRoleRequest req, CancellationToken ct)
    {
        if (id == currentUser.RequiredId)
            return Problem(detail: "You cannot change your own role.", statusCode: StatusCodes.Status400BadRequest);

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (account is null) return NotFound();

        account.Role = req.Role!.Value;
        await db.SaveChangesAsync(ct);
        return Ok(account.ToResponse());
    }
}
