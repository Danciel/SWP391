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
        private readonly IExecutorPricingService _pricing;

        public ExecutorsController(IWillService willService, IExecutorPricingService pricing)
        {
            _willService = willService;
            _pricing = pricing;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVerified()
        {
            var list = await _willService.GetExecutorsAsync();
            var items = list.ToList();
            await _pricing.EnrichListAsync(items);
            return Ok(items);
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
