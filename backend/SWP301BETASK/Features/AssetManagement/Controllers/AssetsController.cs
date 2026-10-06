using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP301BETASK.Features.AssetManagement.Contracts;
using SWP301BETASK.Features.AssetManagement.DTOs;

namespace SWP301BETASK.Features.AssetManagement.Controllers
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


        // =====================================================
        // GET ACCOUNT ID FROM JWT
        // =====================================================

        private bool TryGetAccountId(out Guid accountId)
        {
            accountId = Guid.Empty;

            var value =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(value, out accountId);
        }


        // =====================================================
        // VERIFY EXTERNAL ASSET
        // POST /api/assets/verify
        // =====================================================

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(
            VerifyAssetRequest request)
        {
            var result =
                await _assetService.VerifyAsync(request);

            return Ok(result);
        }


        // =====================================================
        // CREATE
        // POST /api/assets
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateAssetRequest request)
        {
            if (!TryGetAccountId(out var accountId))
            {
                return Unauthorized();
            }

            try
            {
                var result =
                    await _assetService.CreateAsync(
                        accountId,
                        request);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


        // =====================================================
        // GET ALL
        // GET /api/assets
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetAccountId(out var accountId))
            {
                return Unauthorized();
            }

            var result =
                await _assetService.GetAllAsync(accountId);

            return Ok(result);
        }


        // =====================================================
        // GET BY ID
        // GET /api/assets/{id}
        // =====================================================

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!TryGetAccountId(out var accountId))
            {
                return Unauthorized();
            }

            var result =
                await _assetService.GetByIdAsync(
                    accountId,
                    id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }


        // =====================================================
        // REVEAL SECRET
        // GET /api/assets/{id}/secret
        // =====================================================

        [HttpGet("{id:guid}/secret")]
        public async Task<IActionResult> GetSecret(Guid id)
        {
            if (!TryGetAccountId(out var accountId))
            {
                return Unauthorized();
            }

            var result =
                await _assetService.GetSecretAsync(
                    accountId,
                    id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }


        // =====================================================
        // UPDATE
        // PUT /api/assets/{id}
        // =====================================================

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateAssetRequest request)
        {
            if (!TryGetAccountId(out var accountId))
            {
                return Unauthorized();
            }

            try
            {
                var result =
                    await _assetService.UpdateAsync(
                        accountId,
                        id,
                        request);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


        // =====================================================
        // DELETE
        // DELETE /api/assets/{id}
        // =====================================================

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!TryGetAccountId(out var accountId))
            {
                return Unauthorized();
            }

            bool deleted =
                await _assetService.DeleteAsync(
                    accountId,
                    id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}