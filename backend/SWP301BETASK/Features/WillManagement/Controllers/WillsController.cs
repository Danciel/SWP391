using Microsoft.AspNetCore.Mvc;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/wills")]
    public class WillsController : ControllerBase
    {
        private readonly IWillService _wills;
        private readonly ICurrentUser _me;

        public WillsController(IWillService wills, ICurrentUser me)
        {
            _wills = wills;
            _me = me;
        }

        // POST: api/wills
        [HttpPost]
        public async Task<ActionResult<WillDto>> Create([FromBody] CreateWillRequest req, CancellationToken ct)
        {
            var dto = await _wills.CreateAsync(_me.UserId, req, ct);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        // GET: api/wills
        [HttpGet]
        public async Task<ActionResult<List<WillDto>>> List(CancellationToken ct) =>
            Ok(await _wills.ListAsync(_me.UserId, ct));

        // GET: api/wills/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<WillDto>> Get(Guid id, CancellationToken ct) =>
            Ok(await _wills.GetAsync(_me.UserId, id, ct));

        // PUT: api/wills/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<WillDto>> Update(Guid id, [FromBody] UpdateWillRequest req, CancellationToken ct) =>
            Ok(await _wills.UpdateAsync(_me.UserId, id, req, ct));

        // DELETE: api/wills/{id}   (draft only)
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _wills.DeleteDraftAsync(_me.UserId, id, ct);
            return NoContent();
        }

        // PUT: api/wills/{id}/assets
        [HttpPut("{id:guid}/assets")]
        public async Task<ActionResult<WillDto>> SetAssets(Guid id, [FromBody] SetWillAssetsRequest req, CancellationToken ct) =>
            Ok(await _wills.SetAssetsAsync(_me.UserId, id, req, ct));
    }
}
