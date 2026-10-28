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
        private readonly ICurrentUserService _user;

        public HandoverController(
            IHandoverService service,
            ICurrentUserService user)
        {
            _service = service;
            _user = user;
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
