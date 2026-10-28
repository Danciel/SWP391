using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.Services;
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
    private readonly IExecutorPricingService _pricing;

    public ExecutorCsvController(
        LegacyVaultDbContext db,
        ICurrentUserService user,
        IExecutorPricingService pricing)
    {
        _db = db;
        _user = user;
        _pricing = pricing;
    }

    // Sample file the Executor can fill in.
    [HttpGet("template")]
    public IActionResult Template()
        => File(System.Text.Encoding.UTF8.GetBytes(ExecutorCsvParser.TemplateText),
                "text/csv", "executor-evaluation-template.csv");

    // Upload -> parse -> store the file for the Admin -> update evaluation -> return new suggestion.
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

        var parsed = ExecutorCsvParser.Parse(bytes);
        if (!parsed.Ok)
            return BadRequest(new { message = parsed.Errors[0], errors = parsed.Errors });

        var me = _user.GetRequiredAccountId();
        var profile = await _db.ExecutorProfiles.FirstOrDefaultAsync(x => x.AccountId == me);
        if (profile == null)
            return BadRequest(new { message = "Executor profile was not found." });

        var now = DateTimeOffset.UtcNow;
        var stats = parsed.Stats!;

        var submission = new ExecutorCsvSubmission
        {
            Id = Guid.NewGuid(),
            ExecutorAccountId = me,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = "text/csv",
            FileContent = bytes,
            UploadedAt = now
        };
        _db.ExecutorCsvSubmissions.Add(submission);

        profile.EvalExperienceYears = stats.ExperienceYears;
        profile.EvalRating = stats.Rating;
        profile.EvalTotalCases = stats.TotalCases;
        profile.EvalWonCases = stats.WonCases;
        profile.EvalSource = "CSV";
        profile.EvalUpdatedAt = now;
        profile.UpdatedAt = now;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorAccountId = me,
            Action = "EXECUTOR_EVALUATION_CSV_APPLIED",
            EntityType = "ExecutorProfile",
            EntityId = me,
            Details = System.Text.Json.JsonSerializer.Serialize(stats),
            CreatedAt = now
        });

        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            submission.Id,
            submission.OriginalFileName,
            submission.UploadedAt,
            suggestion = await _pricing.GetMineAsync(me)
        });
    }
}
