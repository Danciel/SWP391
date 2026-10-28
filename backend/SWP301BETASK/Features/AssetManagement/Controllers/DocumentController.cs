using System.Security.Cryptography;
using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.AssetManagement.DTOs;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AssetManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/assets/documents")]
public class DocumentController : ControllerBase
{
    private const int MaxBytes = 2 * 1024 * 1024; // Demo: 2 MiB
    private readonly IAssetService _assets;
    private readonly ICurrentUserService _currentUser;
    private readonly LegacyVaultDbContext _db;

    public DocumentController(IAssetService assets, ICurrentUserService currentUser, LegacyVaultDbContext db)
    {
        _assets = assets;
        _currentUser = currentUser;
        _db = db;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file,
        [FromForm] string? name, [FromForm] string? description)
    {
        if (file == null || file.Length == 0 || file.Length > MaxBytes)
            return BadRequest(new { message = "Choose a file smaller than 2 MiB." });

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        byte[] bytes = stream.ToArray();
        string fileName = Path.GetFileName(file.FileName);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string contentType;
        if (extension == ".pdf" && bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            contentType = "application/pdf";
        else if (extension == ".png" && bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}))
            contentType = "image/png";
        else if ((extension == ".jpg" || extension == ".jpeg")
                 && bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            contentType = "image/jpeg";
        else
            return BadRequest(new { message = "Only valid PDF, PNG and JPEG files are supported." });

        string title = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(fileName) : name.Trim();
        if (title.Length == 0 || title.Length > 150)
            return BadRequest(new { message = "Document name must be 1–150 characters." });
        string hash = Convert.ToHexString(SHA256.HashData(bytes));

        // Store file bytes INSIDE the existing AES-GCM encrypted AssetSecret.
        // This avoids plaintext storage and does not require a new table in the demo.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var asset = await _assets.CreateAsync(new CreateAssetRequest
        {
            Name = title, Description = description, AssetType = "DOCUMENT", Provider = "LOCAL_DOCUMENT",
            SecretData = new Dictionary<string, string>
            {
                ["fileName"] = fileName, ["contentType"] = contentType,
                ["sha256"] = hash, ["fileBase64"] = Convert.ToBase64String(bytes)
            }
        });
        var tracked = await _db.Assets.FindAsync(asset.Id);
        if (tracked == null) throw new InvalidOperationException("Document asset not found.");
        tracked.VerificationStatus = "INTEGRITY_VERIFIED";
        tracked.UpdatedAt = DateTimeOffset.UtcNow;
        _db.AssetVerifications.Add(new AssetVerification
        {
            Id = Guid.NewGuid(), AssetId = asset.Id, Provider = "LOCAL_DOCUMENT",
            ExternalIdentifier = hash, Status = "INTEGRITY_VERIFIED",
            VerifiedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return StatusCode(201, new { assetId = asset.Id, fileName, sha256 = hash, verificationStatus = "INTEGRITY_VERIFIED" });
    }

    [HttpGet("{id:guid}/integrity")]
    public async Task<IActionResult> Integrity(Guid id)
    {
        var payload = await LoadOwnedDocument(id);
        if (payload is null) return NotFound();
        bool matches = string.Equals(payload.Value.Hash,
            Convert.ToHexString(SHA256.HashData(payload.Value.Data)), StringComparison.OrdinalIgnoreCase);
        return Ok(new { assetId = id, integrityVerified = matches, verificationStatus = matches ? "INTEGRITY_VERIFIED" : "INTEGRITY_FAILED" });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var payload = await LoadOwnedDocument(id);
        if (payload is null) return NotFound();
        if (!string.Equals(payload.Value.Hash, Convert.ToHexString(SHA256.HashData(payload.Value.Data)), StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "File integrity check failed." });
        Response.Headers.CacheControl = "no-store";
        return File(payload.Value.Data, payload.Value.ContentType, payload.Value.FileName);
    }

    private async Task<(byte[] Data, string Hash, string FileName, string ContentType)?> LoadOwnedDocument(Guid id)
    {
        Guid ownerId = _currentUser.GetRequiredAccountId();
        var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == id && x.OwnerAccountId == ownerId && x.AssetType == "DOCUMENT" && x.Provider == "LOCAL_DOCUMENT");
        if (asset == null) return null;
        var secret = await _assets.RevealSecretAsync(id); // decrypt and authorize owner
        var d = secret.SecretData;
        if (!d.TryGetValue("fileBase64", out var base64) || !d.TryGetValue("sha256", out var hash))
            return null;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64); }
        catch (FormatException) { return null; }
        var fileName = d.TryGetValue("fileName", out var fn) ? Path.GetFileName(fn) : "document";
        var contentType = d.TryGetValue("contentType", out var ct) ? ct : "application/octet-stream";
        return (bytes, hash, fileName, contentType);
    }
}
