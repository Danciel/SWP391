using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Features.WillManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    // LV-20 profile and LV-22 reviews. Same route prefix as ExecutorsController.
    [ApiController]
    [Route("api/executors")]
    [Authorize]
    public class ExecutorProfileController : ControllerBase
    {
        private readonly IExecutorProfileService _service;

        public ExecutorProfileController(IExecutorProfileService service)
        {
            _service = service;
        }

        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateMine(
            [FromBody] UpdateExecutorProfileRequest request)
        {
            try { return Ok(await _service.UpdateMyProfileAsync(request)); }
            catch (InvalidOperationException ex)
            { return BadRequest(new { message = ex.Message }); }
        }

        // E10: reference prices (platform data only) for the comparison view.
        [HttpGet("pricing-reference")]
        public async Task<IActionResult> PricingReference()
            => Ok(await _service.GetPricingReferenceAsync());

        [HttpGet("{accountId:guid}")]
        public async Task<IActionResult> Get(Guid accountId)
        {
            try { return Ok(await _service.GetProfileAsync(accountId)); }
            catch (KeyNotFoundException ex)
            { return NotFound(new { message = ex.Message }); }
        }

        [HttpGet("{accountId:guid}/reviews")]
        public async Task<IActionResult> Reviews(Guid accountId)
            => Ok(await _service.GetReviewsAsync(accountId));

        [HttpPost("{accountId:guid}/reviews")]
        public async Task<IActionResult> CreateReview(
            Guid accountId,
            [FromBody] CreateExecutorReviewRequest request)
        {
            try
            {
                return StatusCode(201,
                    await _service.CreateReviewAsync(accountId, request));
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
