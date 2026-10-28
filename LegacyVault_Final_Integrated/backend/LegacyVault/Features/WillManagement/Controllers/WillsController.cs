using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Features.WillManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LegacyVault.Shared.Security;

namespace LegacyVault.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/wills")]
    [Authorize]
    public class WillsController : ControllerBase
    {

        private readonly IWillService _willService;
        private readonly ICurrentUserService _currentUser;
        private readonly OwnerPinGrantService _pinGrantService;

        public WillsController(
            IWillService willService,
            ICurrentUserService currentUser,
            OwnerPinGrantService pinGrantService)
        {
            _willService = willService;
            _currentUser = currentUser;
            _pinGrantService = pinGrantService;
        }


        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateWillRequest request)
        {
            var result =
                await _willService.CreateAsync(request);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            return Ok(
                await _willService.GetMineAsync());
        }



        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                // B1. Lay danh sach Will cua Owner
                var myWills = await _willService.GetMineAsync();

                // B2. Tim Will duoc yeu cau
                var summary = myWills.FirstOrDefault(
                    w => w.Id == id
                );

                if (summary == null)
                {
                    return NotFound(new
                    {
                        message = "Will not found."
                    });
                }

                // B3. Draft duoc doc de Create Will hoat dong
                bool isDraft = string.Equals(
                    summary.Status,
                    "DRAFT",
                    StringComparison.OrdinalIgnoreCase
                );

                // B4. Will khong phai Draft phai xac minh PIN
                if (!isDraft)
                {
                    Guid ownerId =
                        _currentUser.GetRequiredAccountId();

                    string grant = Request.Headers[
                        "X-Owner-Pin-Grant"
                    ].ToString();

                    bool allowed =
                        _pinGrantService.IsValidGrant(
                            grant,
                            ownerId,
                            OwnerPinScopes.WillDetail,
                            id
                        );

                    if (!allowed)
                    {
                        return StatusCode(423, new
                        {
                            message =
                                "Please verify your 6-digit security PIN."
                        });
                    }
                }

                // B5. Chi tra chi tiet sau khi du dieu kien
                var detail = await _willService.GetByIdAsync(id);

                return Ok(detail);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }



        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateWillRequest request)
        {
            try
            {
                return Ok(
                    await _willService.UpdateAsync(
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
                await _willService.DeleteAsync(id);
                return NoContent();
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

        [HttpPost("{id:guid}/assets")]
        public async Task<IActionResult> AddAsset(
            Guid id,
            [FromBody] AddWillAssetRequest request)
        {
            try
            {
                return Ok(
                    await _willService.AddAssetAsync(
                        id,
                        request));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
            catch (WillConflictException ex)
            {
                return Conflict(
                    new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}/assets/{willAssetId:guid}")]
        public async Task<IActionResult> RemoveAsset(
            Guid id,
            Guid willAssetId)
        {
            try
            {
                await _willService.RemoveAssetAsync(id, willAssetId);
                return NoContent();
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

        [HttpDelete("{id:guid}/assignments/{assignmentId:guid}")]
        public async Task<IActionResult> RemoveAssignment(
            Guid id,
            Guid assignmentId)
        {
            try
            {
                await _willService.RemoveAssignmentAsync(id, assignmentId);
                return NoContent();
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

        [HttpGet("locked-assets")]
        public async Task<IActionResult> GetLockedAssets()
        {
            return Ok(
                await _willService.GetLockedAssetIdsAsync());
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            try
            {
                return Ok(await _willService.CancelAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
            }
            catch (WillConflictException ex)
            {
                return Conflict(
                    new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/beneficiaries/{beneficiaryId:guid}/link")]
        public async Task<IActionResult> LinkBeneficiary(
            Guid id,
            Guid beneficiaryId,
            [FromBody] LinkBeneficiaryRequest request)
        {
            try
            {
                return Ok(
                    await _willService.LinkBeneficiaryAsync(
                        id,
                        beneficiaryId,
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

        [HttpPost("{id:guid}/beneficiaries")]
        public async Task<IActionResult> AddBeneficiary(
            Guid id,
            [FromBody] CreateBeneficiaryRequest request)
        {
            try
            {
                return Ok(
                    await _willService
                        .AddBeneficiaryAsync(
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

        [HttpPost("{id:guid}/assignments")]
        public async Task<IActionResult> AssignAsset(
            Guid id,
            [FromBody]
                CreateAssetAssignmentRequest request)
        {
            try
            {
                return Ok(
                    await _willService
                        .AssignAssetAsync(
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

        [HttpPut("{id:guid}/assignments/{assignmentId:guid}")]
        public async Task<IActionResult> UpdateAssetAssignment(
            Guid id, Guid assignmentId, [FromBody] UpdateAssetAssignmentRequest request)
        {
            try
            {
                return Ok(await _willService.UpdateAssetAssignmentAsync(id, assignmentId, request));
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

        [HttpPost("{id:guid}/executor")]
        public async Task<IActionResult> AssignExecutor(
            Guid id,
            [FromBody] AssignExecutorRequest request)
        {
            try
            {
                return Ok(
                    await _willService
                        .AssignExecutorAsync(
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

        // Optional specialist executor for ONE asset (DRAFT only).
        [HttpPost("{id:guid}/assets/{willAssetId:guid}/executor")]
        public async Task<IActionResult> AssignAssetExecutor(
            Guid id,
            Guid willAssetId,
            [FromBody] AssignAssetExecutorRequest request)
        {
            try
            {
                return Ok(
                    await _willService.AssignAssetExecutorAsync(
                        id, willAssetId, request));
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

        // Back to "use the Will executor" for this asset.
        [HttpDelete("{id:guid}/assets/{willAssetId:guid}/executor")]
        public async Task<IActionResult> ClearAssetExecutor(
            Guid id,
            Guid willAssetId)
        {
            try
            {
                await _willService.ClearAssetExecutorAsync(id, willAssetId);
                return NoContent();
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

        [HttpPut("{id:guid}/dms")]
        public async Task<IActionResult> ConfigureDms(
            Guid id,
            [FromBody] ConfigureDmsRequest request)
        {
            try
            {
                return Ok(
                    await _willService
                        .ConfigureDmsAsync(
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

        [HttpPost("{id:guid}/dms/check-in")]
        public async Task<IActionResult> CheckIn(Guid id)
        {
            try
            {
                return Ok(
                    await _willService.CheckInAsync(id));
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

        [HttpPost("{id:guid}/submit")]
        public async Task<IActionResult> Submit(Guid id)
        {
            try
            {
                return Ok(
                    await _willService.SubmitAsync(id));
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
