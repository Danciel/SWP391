using Microsoft.AspNetCore.Mvc;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/wills/{willId:guid}")]
    public class WillBeneficiariesController : ControllerBase
    {
        private readonly IBeneficiaryService _beneficiaries;
        private readonly ICurrentUser _me;

        public WillBeneficiariesController(IBeneficiaryService beneficiaries, ICurrentUser me)
        {
            _beneficiaries = beneficiaries;
            _me = me;
        }

        // POST: api/wills/{willId}/beneficiaries
        [HttpPost("beneficiaries")]
        public async Task<ActionResult<WillDto>> Add(Guid willId, [FromBody] BeneficiaryRequest req, CancellationToken ct) =>
            Ok(await _beneficiaries.AddAsync(_me.UserId, willId, req, ct));

        // PUT: api/wills/{willId}/beneficiaries/{beneficiaryId}
        [HttpPut("beneficiaries/{beneficiaryId:guid}")]
        public async Task<ActionResult<WillDto>> Update(Guid willId, Guid beneficiaryId, [FromBody] BeneficiaryRequest req, CancellationToken ct) =>
            Ok(await _beneficiaries.UpdateAsync(_me.UserId, willId, beneficiaryId, req, ct));

        // DELETE: api/wills/{willId}/beneficiaries/{beneficiaryId}
        [HttpDelete("beneficiaries/{beneficiaryId:guid}")]
        public async Task<ActionResult<WillDto>> Remove(Guid willId, Guid beneficiaryId, CancellationToken ct) =>
            Ok(await _beneficiaries.RemoveAsync(_me.UserId, willId, beneficiaryId, ct));

        // POST: api/wills/{willId}/assignments
        [HttpPost("assignments")]
        public async Task<ActionResult<WillDto>> Assign(Guid willId, [FromBody] AssignAssetRequest req, CancellationToken ct) =>
            Ok(await _beneficiaries.AssignAsync(_me.UserId, willId, req, ct));
    }
}
