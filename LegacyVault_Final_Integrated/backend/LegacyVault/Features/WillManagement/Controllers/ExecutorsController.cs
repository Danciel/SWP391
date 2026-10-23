using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
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

        // Executor updates the advertised fee for handling one whole Will.
        [HttpPut("me/pricing")]
        public async Task<IActionResult> UpdateMyPricing(
            [FromBody] UpdateExecutorPricingRequest request)
        {
            try
            {
                return Ok(await _willService.UpdateMyExecutorPricingAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
