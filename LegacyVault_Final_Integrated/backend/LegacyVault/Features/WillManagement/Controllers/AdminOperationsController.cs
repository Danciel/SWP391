using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

// Sprint 4 administration/reporting endpoints. ADMIN can observe and suspend,
// but cannot make legal-verifier decisions or modify LifeStatus.
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminOperationsController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _user;

    public AdminOperationsController(LegacyVaultDbContext db, ICurrentUserService user)
    {
        _db = db;
        _user = user;
    }

    // AD05: Executor directory and oversight.
    [HttpGet("executors")]
    public async Task<IActionResult> Executors([FromQuery] bool? suspended = null, [FromQuery] bool? verified = null)
    {
        var query = _db.ExecutorProfiles.AsNoTracking().Include(x => x.Account).AsQueryable();
        if (suspended.HasValue) query = query.Where(x => x.IsSuspended == suspended.Value);
        if (verified.HasValue) query = query.Where(x => x.IsVerified == verified.Value);

        var rows = await query.OrderBy(x => x.IsSuspended).ThenByDescending(x => x.Rating)
            .Select(x => new
            {
                accountId = x.AccountId,
                fullName = x.Account != null ? x.Account.FullName : string.Empty,
                email = x.Account != null ? x.Account.Email : string.Empty,
                role = x.Account != null ? x.Account.Role : string.Empty,
                x.IsVerified,
                x.IsSuspended,
                x.SuspensionReason,
                x.SuspendedAt,
                x.SuspendedByAccountId,
                x.Rating,
                x.CompletedCases,
                x.ExperienceYears,
                x.Specialty,
                x.ServiceFee,
                x.Currency,
                x.UpdatedAt
            }).ToListAsync();
        return Ok(rows);
    }

    // AD05: suspension is reversible and audited. Existing completed work is not rewritten.
    [HttpPut("executors/{accountId:guid}/suspension")]
    public async Task<IActionResult> SetExecutorSuspension(Guid accountId, [FromBody] ExecutorSuspensionRequest request)
    {
        if (request.IsSuspended && string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "A reason is required when suspending an executor." });

        var profile = await _db.ExecutorProfiles.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (profile == null) return NotFound(new { message = "Executor profile not found." });

        var actorId = _user.GetRequiredAccountId();
        var now = DateTimeOffset.UtcNow;
        profile.IsSuspended = request.IsSuspended;
        profile.SuspensionReason = request.IsSuspended ? request.Reason!.Trim() : null;
        profile.SuspendedAt = request.IsSuspended ? now : null;
        profile.SuspendedByAccountId = request.IsSuspended ? actorId : null;
        profile.UpdatedAt = now;
        AddAudit(actorId, request.IsSuspended ? "EXECUTOR_SUSPENDED" : "EXECUTOR_REINSTATED",
            "ExecutorProfile", accountId, new { reason = profile.SuspensionReason });
        await _db.SaveChangesAsync();

        return Ok(new { accountId, profile.IsSuspended, profile.SuspensionReason, profile.SuspendedAt, profile.UpdatedAt });
    }

    // AD07: Legal oversight is read-only; decisions remain exclusive to LEGAL_VERIFIER.
    [HttpGet("legal-cases")]
    public async Task<IActionResult> LegalCases([FromQuery] string? status = null, [FromQuery] Guid? willId = null)
    {
        var query = _db.LegalCases.AsNoTracking()
            .Include(x => x.Will).ThenInclude(x => x!.OwnerAccount)
            .Include(x => x.ExecutorAccount)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        if (willId.HasValue) query = query.Where(x => x.WillId == willId.Value);

        var rows = await query.OrderByDescending(x => x.SubmittedAt).Take(250)
            .Select(x => new
            {
                x.Id,
                x.WillId,
                willTitle = x.Will != null ? x.Will.Title : string.Empty,
                willStatus = x.Will != null ? x.Will.Status : string.Empty,
                ownerName = x.Will != null && x.Will.OwnerAccount != null ? x.Will.OwnerAccount.FullName : string.Empty,
                executorAccountId = x.ExecutorAccountId,
                executorName = x.ExecutorAccount != null ? x.ExecutorAccount.FullName : string.Empty,
                x.OriginalFileName,
                x.ContentType,
                x.Status,
                x.SubmittedAt,
                x.ReviewedAt,
                x.ReviewedByAccountId,
                x.ReviewNote
            }).ToListAsync();
        return Ok(rows);
    }

    // AD08: read-only payment/ledger report. No endpoint here can release or refund money.
    [HttpGet("payments")]
    public async Task<IActionResult> Payments(
        [FromQuery] string? status = null,
        [FromQuery] Guid? willId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.ServicePayments.AsNoTracking()
            .Include(x => x.Will)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        if (willId.HasValue) query = query.Where(x => x.WillId == willId.Value);

        var total = await query.CountAsync();
        var basePayments = await query.OrderByDescending(x => x.HeldAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.WillId,
                willTitle = x.Will != null ? x.Will.Title : string.Empty,
                x.OwnerAccountId,
                ownerName = _db.Accounts.Where(a => a.Id == x.OwnerAccountId).Select(a => a.FullName).FirstOrDefault(),
                x.ExecutorAccountId,
                executorName = _db.Accounts.Where(a => a.Id == x.ExecutorAccountId).Select(a => a.FullName).FirstOrDefault(),
                x.Amount,
                x.Currency,
                x.Status,
                x.HeldAt,
                x.ReleasedAt,
                x.RefundedAt
            }).ToListAsync();
        var willIds = basePayments.Select(x => x.WillId).Distinct().ToList();
        var ledgerRows = await _db.WalletTransactions.AsNoTracking()
            .Where(t => t.WillId.HasValue && willIds.Contains(t.WillId.Value))
            .OrderBy(t => t.CreatedAt)
            .Select(t => new { t.WillId, t.Id, t.AccountId, t.Type, t.Amount, t.BalanceAfter, t.Note, t.CreatedAt })
            .ToListAsync();
        var ledgerByWill = ledgerRows.GroupBy(t => t.WillId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(t => new
            {
                t.Id, t.AccountId, t.Type, t.Amount, t.BalanceAfter, t.Note, t.CreatedAt
            }).ToList());
        var payments = basePayments.Select(x => new
        {
            x.Id, x.WillId, x.willTitle, x.OwnerAccountId, x.ownerName,
            x.ExecutorAccountId, x.executorName, x.Amount, x.Currency, x.Status,
            x.HeldAt, x.ReleasedAt, x.RefundedAt,
            ledger = ledgerByWill.TryGetValue(x.WillId, out var entries) ? entries.Cast<object>().ToList() : new List<object>()
        }).ToList();
        return Ok(new { page, pageSize, total, items = payments });
    }

    // AD09: searchable, paged audit trail. Details are operational metadata only.
    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? actorAccountId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType.Trim());
        if (actorAccountId.HasValue) query = query.Where(x => x.ActorAccountId == actorAccountId.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);

        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.ActorAccountId,
                actorName = _db.Accounts.Where(a => a.Id == x.ActorAccountId).Select(a => a.FullName).FirstOrDefault(),
                x.Action,
                x.EntityType,
                x.EntityId,
                x.Details,
                x.CreatedAt
            }).ToListAsync();
        return Ok(new { page, pageSize, total, items = rows });
    }

    private void AddAudit(Guid actorId, string action, string entityType, Guid? entityId, object details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorAccountId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = JsonSerializer.Serialize(details),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}

public class ExecutorSuspensionRequest
{
    public bool IsSuspended { get; set; }
    public string? Reason { get; set; }
}
