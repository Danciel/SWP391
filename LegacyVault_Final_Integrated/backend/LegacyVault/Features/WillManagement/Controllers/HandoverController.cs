using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Features.WillManagement.Services;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/handover")]
    [Authorize]
    public class HandoverController : ControllerBase
    {
        private readonly IHandoverService _service;
        private readonly IHandoverCodeService _codes;
        private readonly ICurrentUserService _user;
        private readonly IBeneficiaryAutoLinkService _autoLink;

        public HandoverController(
            IHandoverService service,
            IHandoverCodeService codes,
            ICurrentUserService user,
            IBeneficiaryAutoLinkService autoLink)
        {
            _service = service;
            _codes = codes;
            _user = user;
            _autoLink = autoLink;
        }

        // LV-32: Owner, still alive, presses Immediate Handover.
        // The PIN filter reads the route value named "id" (= Will ID).
        [HttpPost("{id:guid}/immediate")]
        [RequireOwnerPin(OwnerPinScopes.HandoverStart)]
        public Task<IActionResult> Immediate(Guid id)
            => Run(() => _service.StartImmediateAsync(
                id, _user.GetRequiredAccountId()));

        // LV-33: Executor starts handover after legal approval.
        [HttpPost("{willId:guid}/start")]
        public Task<IActionResult> StartAfterDeath(Guid willId)
            => Run(() => _service.StartAfterDeathAsync(
                willId, _user.GetRequiredAccountId()));

        // Owner / Executor see everything; a beneficiary only own items.
        [HttpGet("{willId:guid}")]
        public Task<IActionResult> Get(Guid willId)
            => Run(() => _service.GetAsync(
                willId, _user.GetRequiredAccountId()));

        // Executor hands over everything that is still pending in one go.
        // Every recipient gets their own access code by email.
        [HttpPost("{willId:guid}/deliver-all")]
        public Task<IActionResult> DeliverAll(Guid willId)
            => Run(() => _service.DeliverAllAsync(
                willId, _user.GetRequiredAccountId()));

        // Executor sends a NEW access code to one recipient (5 min cooldown).
        // The old code stops working. The code only ever goes to the recipient's email.
        [HttpPost("{willId:guid}/recipients/{designationId:guid}/resend-code")]
        public async Task<IActionResult> ResendCode(Guid willId, Guid designationId)
        {
            var result = await _codes.ResendByExecutorAsync(
                willId, designationId, _user.GetRequiredAccountId(),
                HttpContext.RequestAborted);

            return result.Success
                ? Ok(new { message = result.Message })
                : StatusCode(result.StatusCode, new
                {
                    message = result.Message,
                    lockedUntil = result.LockedUntil
                });
        }

        // C9: the Executor's handovers, each with the next step to take.
        [HttpGet("mine")]
        public Task<IActionResult> Mine()
            => Run(() => _service.GetExecutorHandoversAsync(
                _user.GetRequiredAccountId()));

        // C9: the Executor cancels a death handover that delivered nothing yet.
        [HttpPost("{willId:guid}/cancel")]
        public Task<IActionResult> Cancel(
            Guid willId, [FromBody] HandoverReasonRequest request)
            => Run(() => _service.CancelDeathHandoverAsync(
                willId, _user.GetRequiredAccountId(), request.Reason.Trim()));

        // C7: the account email matches but the name differs; the Executor links
        // the person by hand, with a written reason.
        [HttpPost("{willId:guid}/recipients/{designationId:guid}/confirm-link")]
        public Task<IActionResult> ConfirmLink(
            Guid willId, Guid designationId,
            [FromBody] ConfirmRecipientLinkRequest request)
            => Run(async () =>
            {
                var me = _user.GetRequiredAccountId();
                await _autoLink.ConfirmByExecutorAsync(
                    willId, designationId, me, request.Note);
                return await _service.GetAsync(willId, me);
            });

        // C2: minutes (content, hash, signature status) of the latest handover.
        [HttpGet("{willId:guid}/minutes")]
        public Task<IActionResult> GetMinutes(Guid willId)
            => Run(() => _service.GetMinutesAsync(
                willId, _user.GetRequiredAccountId(), User.IsInRole("ADMIN")));

        // C2: sealed PDF. Owner, Executor and Admin only.
        [HttpGet("{willId:guid}/minutes/pdf")]
        public async Task<IActionResult> GetMinutesPdf(Guid willId)
        {
            try
            {
                var file = await _service.GetMinutesPdfAsync(
                    willId, _user.GetRequiredAccountId(), User.IsInRole("ADMIN"));

                Response.Headers["X-Content-SHA256"] = file.Sha256;
                return File(file.Pdf, "application/pdf", file.FileName);
            }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
        }

        // C3: the Executor signs. Without it no item can be delivered (C4).
        [HttpPost("{willId:guid}/minutes/executor-sign")]
        public Task<IActionResult> ExecutorSign(Guid willId)
            => Run(() => _service.SignAsExecutorAsync(
                willId,
                _user.GetRequiredAccountId(),
                HttpContext.Connection.RemoteIpAddress?.ToString()));

        // Executor marks one asset-to-beneficiary item as delivered.
        [HttpPost("items/{itemId:guid}/deliver")]
        public Task<IActionResult> Deliver(Guid itemId)
            => Run(() => _service.DeliverAsync(
                itemId, _user.GetRequiredAccountId()));

        // Admin: stop an unfinished handover, refund the held fee.
        [HttpPost("{willId:guid}/abort")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Abort(Guid willId)
            => Run(() => _service.AbortAsync(
                willId, _user.GetRequiredAccountId()));

        // H06: Admin pauses a handover (a dispute pauses it automatically).
        [HttpPost("{willId:guid}/pause")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Pause(
            Guid willId, [FromBody] HandoverReasonRequest request)
            => Run(() => _service.PauseAsync(
                willId, _user.GetRequiredAccountId(), request.Reason.Trim()));

        // H06: Admin continues a paused handover (no open dispute allowed).
        [HttpPost("{willId:guid}/resume")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Resume(Guid willId)
            => Run(() => _service.ResumeAsync(
                willId, _user.GetRequiredAccountId()));

        // H06 revise: send one delivered item back to PENDING while paused.
        [HttpPost("items/{itemId:guid}/reopen")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Reopen(
            Guid itemId, [FromBody] HandoverReasonRequest request)
            => Run(() => _service.ReopenItemAsync(
                itemId, _user.GetRequiredAccountId(), request.Reason.Trim()));

        // H06 reassign: another executor for an ACTIVE / paused will.
        [HttpPost("{willId:guid}/reassign-executor")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Reassign(
            Guid willId, [FromBody] ReassignExecutorRequest request)
            => Run(() => _service.ReassignExecutorAsync(
                willId,
                request.NewExecutorAccountId,
                _user.GetRequiredAccountId(),
                request.Reason.Trim()));

        private async Task<IActionResult> Run<T>(Func<Task<T>> action)
        {
            try { return Ok(await action()); }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (WillConflictException ex)
            { return Conflict(new { message = ex.Message }); }
            catch (InvalidOperationException ex)
            { return BadRequest(new { message = ex.Message }); }
        }
    }
}
