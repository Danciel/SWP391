using LegacyVault.Features.WillManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/executors")]
    [Authorize]
    public class ExecutorsController : ControllerBase
    {
        private readonly IWillService _willService;

        public ExecutorsController(IWillService willService)
        {
            _willService = willService;
        }

        [HttpGet]
        public async Task<IActionResult> GetVerified()
        {
            return Ok(
                await _willService.GetExecutorsAsync());
        }
    }
}
