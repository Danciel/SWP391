using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

[ApiController]
[Authorize]
public class PaymentDisputesController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IWalletService _wallet;
    private readonly IHandoverService _handover;

    public PaymentDisputesController(
        LegacyVaultDbContext db,
        ICurrentUserService user,
        IWalletService wallet,
        IHandoverService handover)
    {
        _db = db;
        _user = user;
        _wallet = wallet;
        _handover = handover;
    }

    // F06: either the Owner or the assigned Executor can raise one dispute per Will.
    [HttpPost("api/wallet/disputes")]
    public async Task<IActionResult> Open([FromBody] OpenPaymentDisputeRequest request)
    {
        var me = _user.GetRequiredAccountId();
        var payment = await _db.ServicePayments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.WillId == request.WillId);
        if (payment == null) return NotFound(new { message = "Payment not found for this Will." });

        var isOwner = payment.OwnerAccountId == me;
        var isExecutor = payment.ExecutorAccountId == me;
        if (!isOwner && !isExecutor) return NotFound(new { message = "Payment not found for this Will." });
        if (payment.Status != "HELD")
            return Conflict(new { message = "A dispute can be opened only while the Executor fee is held." });

        var now = DateTimeOffset.UtcNow;
        var reason = request.Reason.Trim();
        var existing = await _db.PaymentDisputes
            .FirstOrDefaultAsync(x => x.WillId == request.WillId);

        PaymentDispute dispute;
        if (existing != null)
        {
            if (existing.Status == "OPEN")
                return Conflict(new { message = "A dispute is already open for this Will." });

            // One row per Will (unique index). A resolved dispute is reopened;
            // its previous outcome stays in the audit trail.
            AddAudit(me, "PAYMENT_DISPUTE_REOPENED", "PaymentDispute", existing.Id,
                new { previousAction = existing.ResolutionAction, previousNote = existing.ResolutionNote });
            existing.RaisedByAccountId = me;
            existing.Reason = reason;
            existing.Status = "OPEN";
            existing.ResolutionAction = null;
            existing.ResolutionNote = null;
            existing.ResolvedByAccountId = null;
            existing.ResolvedAt = null;
            existing.CreatedAt = now;
            dispute = existing;
        }
        else
        {
            dispute = new PaymentDispute
            {
                Id = Guid.NewGuid(),
                WillId = request.WillId,
                RaisedByAccountId = me,
                Reason = reason,
                Status = "OPEN",
                CreatedAt = now
            };
            _db.PaymentDisputes.Add(dispute);
        }

        AddAudit(me, "PAYMENT_DISPUTE_OPENED", "PaymentDispute", dispute.Id,
            new { dispute.WillId, dispute.Reason });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "A dispute already exists for this Will." });
        }

        // H06: an unfinished handover stops until the dispute is resolved.
        var paused = false;
        try
        {
            await _handover.PauseAsync(request.WillId, me, "Payment dispute opened");
            paused = true;
        }
        catch (Exception ex) when (ex is KeyNotFoundException or WillConflictException)
        {
            // No handover running (or it just finished): nothing to pause.
        }

        return StatusCode(201, new { dispute.Id, dispute.WillId, dispute.Status, dispute.Reason, dispute.CreatedAt, handoverPaused = paused });
    }

    [HttpGet("api/wallet/disputes/mine")]
    public async Task<IActionResult> Mine()
    {
        var me = _user.GetRequiredAccountId();
        var rows = await _db.PaymentDisputes.AsNoTracking()
            .Where(x => x.RaisedByAccountId == me)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.WillId, x.Reason, x.Status, x.ResolutionAction, x.ResolutionNote, x.CreatedAt, x.ResolvedAt })
            .ToListAsync();
        return Ok(rows);
    }

    // AD08/F06: admin can inspect disputes; only this endpoint resolves them.
    [HttpGet("api/admin/payment-disputes")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ListAdmin([FromQuery] string? status = "OPEN")
    {
        var query = _db.PaymentDisputes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        var rows = await query.OrderByDescending(x => x.CreatedAt)
            .Take(250)
            .Select(x => new
            {
                x.Id, x.WillId, x.RaisedByAccountId,
                raisedByName = _db.Accounts.Where(a => a.Id == x.RaisedByAccountId).Select(a => a.FullName).FirstOrDefault(),
                x.Reason, x.Status, x.ResolutionAction, x.ResolutionNote,
                x.ResolvedByAccountId, x.CreatedAt, x.ResolvedAt,
                paymentStatus = _db.ServicePayments.Where(p => p.WillId == x.WillId).Select(p => p.Status).FirstOrDefault(),
                willStatus = _db.Wills.Where(w => w.Id == x.WillId).Select(w => w.Status).FirstOrDefault()
            }).ToListAsync();
        return Ok(rows);
    }

    // H06/F06: ADMIN settles a dispute with REFUND, RELEASE, RESUME or REASSIGN.
    // Money only moves through WalletService (idempotent, one winner).
    [HttpPost("api/admin/payment-disputes/{id:guid}/resolve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolvePaymentDisputeRequest request)
    {
        var action = request.Action.Trim().ToUpperInvariant();
        if (action is not ("REFUND" or "RELEASE" or "RESUME" or "REASSIGN"))
            return BadRequest(new { message = "Action must be REFUND, RELEASE, RESUME or REASSIGN." });
        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new { message = "A resolution note is required." });
        if (action == "REASSIGN" && !request.NewExecutorAccountId.HasValue)
            return BadRequest(new { message = "NewExecutorAccountId is required for REASSIGN." });

        var dispute = await _db.PaymentDisputes.FirstOrDefaultAsync(x => x.Id == id);
        if (dispute == null) return NotFound(new { message = "Payment dispute not found." });
        if (dispute.Status != "OPEN")
            return Conflict(new { message = "This dispute was already resolved." });

        var actor = _user.GetRequiredAccountId();

        try
        {
            var failure = await ApplyResolutionAsync(dispute, action, request, actor);
            if (failure != null) return failure;
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (WillConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        dispute.Status = "RESOLVED";
        dispute.ResolutionAction = action;
        dispute.ResolutionNote = request.Note.Trim();
        dispute.ResolvedByAccountId = actor;
        dispute.ResolvedAt = DateTimeOffset.UtcNow;
        AddAudit(actor, "PAYMENT_DISPUTE_RESOLVED", "PaymentDispute", dispute.Id,
            new { dispute.WillId, action, note = dispute.ResolutionNote, newExecutorAccountId = request.NewExecutorAccountId });
        await _db.SaveChangesAsync();

        // RESUME: if the last item was delivered while paused, the Will is already
        // HANDED_OVER and its fee stayed HELD because of this dispute. Now it is closed.
        if (action == "RESUME")
        {
            var willStatus = await _db.Wills.AsNoTracking()
                .Where(x => x.Id == dispute.WillId).Select(x => x.Status).FirstAsync();
            if (willStatus == "HANDED_OVER")
                await _wallet.ReleaseFeeAsync(dispute.WillId);
        }

        return Ok(new { dispute.Id, dispute.WillId, dispute.Status, dispute.ResolutionAction, dispute.ResolutionNote, dispute.ResolvedAt });
    }

    // Returns null on success, or the error result to send back.
    private async Task<IActionResult?> ApplyResolutionAsync(
        PaymentDispute dispute, string action, ResolvePaymentDisputeRequest request, Guid actor)
    {
        // AsNoTracking: money is changed with ExecuteUpdate, a tracked copy would go stale.
        var payment = await _db.ServicePayments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.WillId == dispute.WillId);
        if (payment == null) return NotFound(new { message = "Payment not found." });
        var willStatus = await _db.Wills.AsNoTracking()
            .Where(x => x.Id == dispute.WillId).Select(x => x.Status).FirstOrDefaultAsync();

        switch (action)
        {
            case "REFUND":
            {
                if (payment.Status == "HELD")
                {
                    var unfinished = await _db.HandoverCases.AnyAsync(x =>
                        x.WillId == dispute.WillId &&
                        (x.Status == "IN_PROGRESS" || x.Status == "PAUSED"));
                    if (unfinished)
                    {
                        try
                        {
                            await _handover.AbortAsync(dispute.WillId, actor);
                        }
                        catch (Exception ex) when (ex is KeyNotFoundException or WillConflictException)
                        {
                            // A concurrent admin may already have aborted/refunded it.
                            var latest = await _db.ServicePayments.AsNoTracking()
                                .Where(x => x.WillId == dispute.WillId).Select(x => x.Status).FirstAsync();
                            if (latest != "REFUNDED")
                                return Conflict(new { message = "Handover changed while resolving this dispute; refresh and retry." });
                        }
                    }
                    else
                    {
                        await _wallet.RefundFeeAsync(dispute.WillId);
                    }

                    var refundStatus = await _db.ServicePayments.AsNoTracking()
                        .Where(x => x.WillId == dispute.WillId).Select(x => x.Status).FirstAsync();
                    if (refundStatus != "REFUNDED")
                        return Conflict(new { message = "Payment was changed concurrently; refresh before resolving the dispute." });
                }
                else if (payment.Status != "REFUNDED")
                {
                    return Conflict(new { message = "A released fee cannot be refunded by this mock dispute workflow." });
                }
                return null;
            }

            case "RELEASE":
            {
                if (willStatus != "HANDED_OVER")
                    return Conflict(new { message = "A fee can be released only after the Will is HANDED_OVER." });
                if (payment.Status == "HELD")
                    await _wallet.ReleaseFeeAsync(dispute.WillId, ignoreOpenDispute: true);
                else if (payment.Status != "RELEASED")
                    return Conflict(new { message = "This fee has already been refunded." });

                var releaseStatus = await _db.ServicePayments.AsNoTracking()
                    .Where(x => x.WillId == dispute.WillId).Select(x => x.Status).FirstAsync();
                if (releaseStatus != "RELEASED")
                    return Conflict(new { message = "Payment was changed concurrently; refresh before resolving the dispute." });
                return null;
            }

            case "RESUME":
                // The fee stays HELD; deliveries continue with the same executor.
                await _handover.ResumeAsync(dispute.WillId, actor, resolvingDispute: true);
                return null;

            default: // REASSIGN
                await _handover.ReassignExecutorAsync(
                    dispute.WillId, request.NewExecutorAccountId!.Value, actor, request.Note.Trim());
                return null;
        }
    }

    private void AddAudit(Guid actorId, string action, string entityType, Guid? entityId, object details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorAccountId = actorId, Action = action,
            EntityType = entityType, EntityId = entityId,
            Details = JsonSerializer.Serialize(details), CreatedAt = DateTimeOffset.UtcNow
        });
    }
}

public class OpenPaymentDisputeRequest
{
    [Required]
    public Guid WillId { get; set; }

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public class ResolvePaymentDisputeRequest
{
    [Required]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Note { get; set; } = string.Empty;

    // Only for REASSIGN.
    public Guid? NewExecutorAccountId { get; set; }
}
