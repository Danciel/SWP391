using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/admin/dms-rule")]
    [Authorize(Roles = "ADMIN")]
    public class AdminDmsRuleController : ControllerBase
    {
        private readonly IDmsRuleService _dmsRuleService;
        private readonly ICurrentUserService _currentUser;

        public AdminDmsRuleController(
            IDmsRuleService dmsRuleService,
            ICurrentUserService currentUser)
        {
            _dmsRuleService = dmsRuleService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var rule = await _dmsRuleService.GetAsync(ct);
            return rule == null
                ? NotFound(new { message = "DMS rule has not been configured." })
                : Ok(rule);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] UpsertDmsRuleRequest request,
            CancellationToken ct)
        {
            try
            {
                var rule = await _dmsRuleService.CreateAsync(
                    request, _currentUser.GetRequiredAccountId(), ct);

                return StatusCode(StatusCodes.Status201Created, rule);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpsertDmsRuleRequest request,
            CancellationToken ct)
        {
            try
            {
                return Ok(await _dmsRuleService.UpdateAsync(
                    request, _currentUser.GetRequiredAccountId(), ct));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(CancellationToken ct)
        {
            try
            {
                await _dmsRuleService.DeleteAsync(ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}