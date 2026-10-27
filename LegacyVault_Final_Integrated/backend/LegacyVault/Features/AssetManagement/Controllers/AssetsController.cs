using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.AssetManagement.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegacyVault.Shared.Security;

namespace LegacyVault.Features.AssetManagement.Controllers
{
    [ApiController]
    [Route("api/assets")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateAssetRequest request)
        {
            try
            {
                var result =
                    await _assetService.CreateAsync(request);

                return StatusCode(
                    StatusCodes.Status201Created,
                    result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            return Ok(
                await _assetService.GetMineAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                return Ok(
                    await _assetService.GetByIdAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        [RequireOwnerPin(OwnerPinScopes.AssetSecret)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateAssetRequest request)
        {
            try
            {
                return Ok(
                    await _assetService.UpdateAsync(
                        id,
                        request));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _assetService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
        }

        // Demo/authorized reveal endpoint.
        // Normal list/detail responses never contain secret data.
        [HttpGet("{id:guid}/secret")]
        [RequireOwnerPin(OwnerPinScopes.AssetSecret)]
        public async Task<IActionResult> RevealSecret(Guid id)

        {
            try
            {
                return Ok(
                    await _assetService
                        .RevealSecretAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/verify")]
        public async Task<IActionResult> Verify(Guid id)
        {
            try
            {
                return Ok(
                    await _assetService.VerifyAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message });
            }
        }
    }
}
