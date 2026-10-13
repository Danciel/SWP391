using Microsoft.AspNetCore.Mvc;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Controllers
{
    [ApiController]
    public class WillExecutorDmsController : ControllerBase
    {
        private readonly IExecutorService _executors;
        private readonly IDmsService _dms;
        private readonly IWillSubmissionService _submission;
        private readonly ICurrentUser _me;

        public WillExecutorDmsController(IExecutorService executors, IDmsService dms,
            IWillSubmissionService submission, ICurrentUser me)
        {
            _executors = executors;
            _dms = dms;
            _submission = submission;
            _me = me;
        }

        // GET: api/executors
        [HttpGet("api/executors")]
        public async Task<ActionResult<List<ExecutorDto>>> ListExecutors(CancellationToken ct) =>
            Ok(await _executors.ListAsync(ct));

        // PUT: api/wills/{id}/executor
        [HttpPut("api/wills/{id:guid}/executor")]
        public async Task<ActionResult<WillDto>> SetExecutor(Guid id, [FromBody] SetExecutorRequest req, CancellationToken ct) =>
            Ok(await _executors.SetAsync(_me.UserId, id, req, ct));

        // PUT: api/wills/{id}/dms
        [HttpPut("api/wills/{id:guid}/dms")]
        public async Task<ActionResult<WillDto>> SetDms(Guid id, [FromBody] SetDmsRequest req, CancellationToken ct) =>
            Ok(await _dms.SetAsync(_me.UserId, id, req, ct));

        // POST: api/wills/{id}/dms/check-in
        [HttpPost("api/wills/{id:guid}/dms/check-in")]
        public async Task<ActionResult<WillDto>> CheckIn(Guid id, CancellationToken ct) =>
            Ok(await _dms.CheckInAsync(_me.UserId, id, ct));

        // GET: api/wills/{id}/review
        [HttpGet("api/wills/{id:guid}/review")]
        public async Task<ActionResult<WillReviewDto>> Review(Guid id, CancellationToken ct) =>
            Ok(await _submission.ReviewAsync(_me.UserId, id, ct));

        // POST: api/wills/{id}/submit
        [HttpPost("api/wills/{id:guid}/submit")]
        public async Task<ActionResult<WillDto>> Submit(Guid id, CancellationToken ct) =>
            Ok(await _submission.SubmitAsync(_me.UserId, id, ct));

        // ---- aliases for the paths the FE ownerApi.js already calls (BASE_URL already ends with /api) ----
        [HttpPost("api/owner/wills/{id:guid}/check-in")]
        public Task<ActionResult<WillDto>> OwnerCheckIn(Guid id, CancellationToken ct) => CheckIn(id, ct);

        [HttpPost("api/owner/wills/{id:guid}/request-handover")]
        public async Task<ActionResult<WillDto>> RequestHandover(Guid id, CancellationToken ct) =>
            Ok(await _dms.RequestHandoverAsync(_me.UserId, id, ct));

        [HttpPost("api/owner/wills/{id:guid}/cancel-handover")]
        public async Task<ActionResult<WillDto>> CancelHandover(Guid id, CancellationToken ct) =>
            Ok(await _dms.CancelHandoverAsync(_me.UserId, id, ct));
    }
}
