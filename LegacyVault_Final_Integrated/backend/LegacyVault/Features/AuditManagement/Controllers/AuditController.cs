using LegacyVault.Features.AuditManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.AuditManagement.Controllers;

[ApiController]
[Route("api/admin/auditlogs")]
[Authorize(Roles = "ADMIN")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? actorAccountId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (items, total) = await _auditService.GetLogsAsync(
            action, entityType, actorAccountId, from, to, page, pageSize);

        return Ok(new { page, pageSize, total, items });
    }
}