using LegacyVault.Data;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Controllers;

[ApiController]
[Route("api/executors/me/personal-csv")]
[Authorize(Roles = "EXECUTOR")]
public class ExecutorCsvController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _user;
    public ExecutorCsvController(LegacyVaultDbContext db, ICurrentUserService user) { _db = db; _user = user; }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Choose a CSV file." });
        if (file.Length > MaxFileSize) return BadRequest(new { message = "CSV file must be 5 MB or smaller." });
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .csv files are accepted." });
        byte[] bytes;
        await using (var stream = new MemoryStream()) { await file.CopyToAsync(stream); bytes = stream.ToArray(); }
        var submission = new ExecutorCsvSubmission { Id = Guid.NewGuid(), ExecutorAccountId = _user.GetRequiredAccountId(),
            OriginalFileName = Path.GetFileName(file.FileName), ContentType = "text/csv", FileContent = bytes, UploadedAt = DateTimeOffset.UtcNow };
        _db.ExecutorCsvSubmissions.Add(submission);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { submission.Id, submission.OriginalFileName, submission.UploadedAt });
    }
}
