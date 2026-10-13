using Microsoft.AspNetCore.Mvc;
using SWP301BETASK.Features.WillManagement.Contracts;

namespace SWP301BETASK.Features.WillManagement.Controllers
{
    public class ExecutorResponseRequest { public bool Accepted { get; set; } }

    /// <summary>
    /// Development-only helpers so the flow can be tested before the Executor dashboard exists.
    /// Every action answers 404 outside Development.
    /// </summary>
    [ApiController]
    [Route("api/dev/wills/{id:guid}")]
    public class WillDevToolsController : ControllerBase
    {
        private readonly IExecutorService _executors;
        private readonly IDmsService _dms;
        private readonly IHostEnvironment _env;

        public WillDevToolsController(IExecutorService executors, IDmsService dms, IHostEnvironment env)
        {
            _executors = executors;
            _dms = dms;
            _env = env;
        }

        // POST: api/dev/wills/{id}/executor-response   { "accepted": true }
        [HttpPost("executor-response")]
        public async Task<IActionResult> ExecutorResponse(Guid id, [FromBody] ExecutorResponseRequest req, CancellationToken ct)
        {
            if (!_env.IsDevelopment()) return NotFound();
            await _executors.ResolveHiringAsync(id, req.Accepted, ct);
            return NoContent();
        }

        // POST: api/dev/wills/run-dms-check?daysAhead=100  -> evaluates overdue DMS as if "daysAhead" days had passed
        [HttpPost("/api/dev/wills/run-dms-check")]
        public async Task<IActionResult> RunDmsCheck([FromQuery] int daysAhead = 0, CancellationToken ct = default)
        {
            if (!_env.IsDevelopment()) return NotFound();
            var triggered = await _dms.EvaluateOverdueAsync(DateTime.UtcNow.AddDays(daysAhead), ct);
            return Ok(new { triggered });
        }
    }
}
