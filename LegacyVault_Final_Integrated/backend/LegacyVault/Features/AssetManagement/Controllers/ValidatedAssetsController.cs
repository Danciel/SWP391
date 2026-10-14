using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.AssetManagement.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AssetManagement.Controllers;

// New endpoint. Existing AssetsController and AssetService are untouched.
[ApiController, Authorize, Route("api/assets/validated")]
public class ValidatedAssetsController : ControllerBase
{
    private readonly LegacyVaultDbContext _db;
    private readonly IAssetService _assets;
    private static readonly HashSet<string> WithoutExternalLogin = new(StringComparer.OrdinalIgnoreCase) { "DOCUMENT", "OTHER" };
    private static readonly HashSet<string> LoginTypes = new(StringComparer.OrdinalIgnoreCase) { "ACCOUNT", "DIGITAL_WALLET", "BANK_ACCOUNT", "CLOUD_STORAGE" };
    public ValidatedAssetsController(LegacyVaultDbContext db, IAssetService assets) { _db = db; _assets = assets; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
    {
        var type = request.AssetType?.Trim().ToUpperInvariant() ?? "";
        if (!LoginTypes.Contains(type) && !WithoutExternalLogin.Contains(type)) return BadRequest(new { message = "Invalid asset type." });
        var provider = (request.Provider ?? type).Trim().ToUpperInvariant();
        if (type != "ACCOUNT" && provider != type) return BadRequest(new { message = "Provider must match AssetType for this asset category." });
        if (string.IsNullOrWhiteSpace(provider)) return BadRequest(new { message = "Provider is required." });
        var secret = request.SecretData ?? new Dictionary<string, string>();
        if (WithoutExternalLogin.Contains(type))
        {
            // Retain existing encryption contract without requiring account credentials.
            if (secret.Count == 0) secret = new Dictionary<string,string> { ["content"] = request.Description ?? request.Name };
        }
        else
        {
            var fields = secret.ToDictionary(k => k.Key.ToLowerInvariant(), v => v.Value);
            fields.TryGetValue("username", out var username);
            fields.TryGetValue("password", out var password);
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Username and password are required." });
            var account = await _db.MockExternalAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Provider == provider && x.Username == username.Trim() && x.IsActive);
            if (account == null || account.Password != password)
                return BadRequest(new { message = "External account does not exist or credentials are incorrect." });
        }
        try
        {
            // Verify BEFORE persisting; no Asset is created when credentials fail.
            return StatusCode(201, await _assets.CreateAsync(new CreateAssetRequest { Name=request.Name, Description=request.Description, AssetType=type, Provider=provider, SecretData=secret }));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }
}
