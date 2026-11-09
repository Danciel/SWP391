using LegacyVault.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

[ApiController]
[Route("api/admin/executor-csv")]
[Authorize(Roles = "ADMIN")]
public class AdminExecutorCsvController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    public AdminExecutorCsvController(LegacyVaultDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await _db.ExecutorCsvSubmissions.AsNoTracking().OrderByDescending(x => x.UploadedAt)
            .Select(x => new { x.Id, x.ExecutorAccountId, executorName = _db.Accounts.Where(a => a.Id == x.ExecutorAccountId).Select(a => a.FullName).FirstOrDefault(), x.OriginalFileName, x.UploadedAt })
            .ToListAsync());

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await _db.ExecutorCsvSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (file == null) return NotFound(new { message = "CSV submission not found." });
        return File(file.FileContent, "text/csv", file.OriginalFileName);
    }
}
