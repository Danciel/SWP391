using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Features.WillManagement.Services;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    // LV-29 (Executor uploads) and LV-30 (Legal Verify decides).
    // ADMIN is intentionally not allowed to decide (LV-39).
    [ApiController]
    [Route("api/legal-cases")]
    [Authorize]
    public class LegalCasesController : ControllerBase
    {
        private const string LegalRole = "LEGAL_VERIFIER";

        private readonly ILegalService _service;
        private readonly ICurrentUserService _user;

        public LegalCasesController(
            ILegalService service,
            ICurrentUserService user)
        {
            _service = service;
            _user = user;
        }

        private bool IsLegal => User.IsInRole(LegalRole);

        // multipart/form-data: willId + file
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Submit(
            [FromForm] Guid willId,
            IFormFile file)
        {
            try
            {
                var result = await _service.SubmitAsync(
                    _user.GetRequiredAccountId(), willId, file);
                return StatusCode(201, result);
            }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (WillConflictException ex)
            { return Conflict(new { message = ex.Message }); }
            catch (InvalidOperationException ex)
            { return BadRequest(new { message = ex.Message }); }
        }

        // Legal sees all; an Executor sees only their own submissions.
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? status)
            => Ok(await _service.ListAsync(
                _user.GetRequiredAccountId(), IsLegal, status));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                return Ok(await _service.GetAsync(
                    id, _user.GetRequiredAccountId(), IsLegal));
            }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
        }

        [HttpGet("{id:guid}/certificate")]
        public async Task<IActionResult> Certificate(Guid id)
        {
            try
            {
                var f = await _service.GetCertificateAsync(
                    id, _user.GetRequiredAccountId(), IsLegal);
                return PhysicalFile(f.Path, f.ContentType, f.FileName);
            }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id:guid}/decision")]
        [Authorize(Roles = LegalRole)]
        public async Task<IActionResult> Decide(
            Guid id,
            [FromBody] LegalDecisionRequest request)
        {
            try
            {
                return Ok(await _service.DecideAsync(
                    id, _user.GetRequiredAccountId(), request));
            }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
            catch (WillConflictException ex)
            { return Conflict(new { message = ex.Message }); }
            catch (InvalidOperationException ex)
            { return BadRequest(new { message = ex.Message }); }
        }
    }
}
