using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.Services;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    // C6: "I'm alive" after a DMS trigger.
    [ApiController]
    [Route("api/wills")]
    [Authorize]
    public class OwnerAliveController : ControllerBase
    {
        private readonly IOwnerAliveService _service;
        private readonly ICurrentUserService _user;

        public OwnerAliveController(
            IOwnerAliveService service,
            ICurrentUserService user)
        {
            _service = service;
            _user = user;
        }

        [HttpPost("{id:guid}/dms/im-alive")]
        public async Task<IActionResult> ImAlive(Guid id, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.ConfirmAliveAsync(
                    id, _user.GetRequiredAccountId(), ct));
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
