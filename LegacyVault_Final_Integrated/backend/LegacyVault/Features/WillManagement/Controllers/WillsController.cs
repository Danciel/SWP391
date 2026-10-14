using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/wills")]
    [Authorize]
    public class WillsController : ControllerBase
    {
        private readonly IWillService _willService;

        public WillsController(IWillService willService)
        {
            _willService = willService;
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
                return Ok(
                    await _willService.GetByIdAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message });
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
