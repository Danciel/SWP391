
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.AssetManagement.DTOs;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AssetManagement.Controllers;

// ========================================
// B1. REQUEST DTO
// ========================================

public sealed class StartAssetConnectionRequest
{
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    // Required for Google Drive
    public string? FileId { get; set; }

    // Gmail address or GitHub username
    public string? ExpectedIdentifier { get; set; }

    // Origin of the local Vite app that initiated this OAuth request.
    public string? FrontendOrigin { get; set; }
}

public sealed class CompleteAssetConnectionRequest
{
    public string Ticket { get; set; } = "";

    // Optional owner-supplied handover details.
    // These do NOT authenticate with Google/GitHub.
    public string? HandoverPassword { get; set; }

    public bool HandoverConsent { get; set; }
}

// ========================================
// B2. OAUTH STATE
// ========================================

internal sealed class PendingAssetConnection
{
    public Guid OwnerId { get; init; }

    public string Provider { get; init; } = "";

    public string Name { get; init; } = "";

    public string? Description { get; init; }

    public string? FileId { get; init; }

    public string? ExpectedIdentifier { get; init; }

    public string FrontendOrigin { get; init; } = "http://localhost:5173";

    public string CodeVerifier { get; init; } = "";

    public string RedirectUri { get; init; } = "";

    public bool Consumed { get; set; }
}

internal sealed class VerifiedAssetConnection
{
    public PendingAssetConnection Pending { get; init; }
        = new();

    public string ExternalIdentifier { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string? AccountIdentifier { get; init; }

    public bool Consumed { get; set; }
}

// ========================================
// B3. CONTROLLER
// ========================================

[ApiController]
[Route("api/assets/connections")]
public class AssetConnectionsController : ControllerBase
{
    private readonly IMemoryCache _cache;

    private readonly IHttpClientFactory _httpFactory;

    private readonly IConfiguration _config;

    private readonly ICurrentUserService _currentUser;

    private readonly IAssetService _assets;

    private readonly LegacyVaultDbContext _db;

    private static readonly TimeSpan SessionLifetime =
        TimeSpan.FromMinutes(10);

    public AssetConnectionsController(
        IMemoryCache cache,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ICurrentUserService currentUser,
        IAssetService assets,
        LegacyVaultDbContext db)
    {
        _cache = cache;
        _httpFactory = httpFactory;
        _config = config;
        _currentUser = currentUser;
        _assets = assets;
        _db = db;
    }

    // ========================================
    // B4. LIST SUPPORTED PROVIDERS
    // ========================================

    [HttpGet("providers")]
    [HttpGet("~/api/assets/providers")]
    [Authorize]
    public IActionResult Providers()
    {
        return Ok(new[]
        {
            new
            {
                assetType = "ACCOUNT",
                provider = "GMAIL",
                method = "OAUTH"
            },
            new
            {
                assetType = "ACCOUNT",
                provider = "GITHUB",
                method = "OAUTH"
            },
            new
            {
                assetType = "CLOUD_STORAGE",
                provider = "GOOGLE_DRIVE",
                method = "OAUTH_FILE"
            },
            new
            {
                assetType = "DOCUMENT",
                provider = "LOCAL_DOCUMENT",
                method = "FILE_INTEGRITY"
            },
        });
    }

    // ========================================
    // B5. START OAUTH
    // ========================================

    [HttpPost("{provider}/start")]
    [Authorize]
    public IActionResult Start(
        string provider,
        [FromBody] StartAssetConnectionRequest request)
    {
        provider = provider.Trim().ToUpperInvariant();

        if (!IsSupported(provider))
        {
            return BadRequest(new
            {
                message = "Unsupported provider."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message =
                    "Asset name must be between 1 and 150 characters."
            });
        }

        if (provider == "GOOGLE_DRIVE"
            && string.IsNullOrWhiteSpace(request.FileId))
        {
            return BadRequest(new
            {
                message =
                    "Google Drive requires a selected fileId."
            });
        }

        if (request.FileId?.Length > 512)
        {
            return BadRequest(new
            {
                message = "Invalid fileId."
            });
        }

        // Gmail / GitHub account matching
        bool needsIdentifier =
            provider is "GMAIL" or "GITHUB";

        string expectedIdentifier =
            request.ExpectedIdentifier?.Trim() ?? "";

        if (needsIdentifier &&
            (expectedIdentifier.Length == 0
             || expectedIdentifier.Length > 254))
        {
            return BadRequest(new
            {
                message =
                    "Account email / username is required (max 254 chars)."
            });
        }

        // Google account email must be a valid declared email address.
        // The verified email returned by Google is checked again in Callback().
        if (provider == "GMAIL")
        {
            bool validGoogleEmail =
                MailAddress.TryCreate(expectedIdentifier, out var googleAddress)
                && string.Equals(googleAddress.Address, expectedIdentifier,
                    StringComparison.OrdinalIgnoreCase)
                && !expectedIdentifier.Any(char.IsWhiteSpace);

            if (!validGoogleEmail)
                return BadRequest(new
                {
                    message = "Enter the Google account email address you want to verify."
                });
        }

        // GitHub allows sign-in with email, but the /user API returns
        // a GitHub login, NOT the email used to sign in.
        // Accept a username (@username is also fine), or an email
        // only if GitHub later confirms that email is verified.
        if (provider == "GITHUB")
        {
            if (expectedIdentifier.StartsWith("@", StringComparison.Ordinal))
                expectedIdentifier = expectedIdentifier[1..];

            bool validGithubIdentifier;
            if (expectedIdentifier.Contains('@'))
            {
                validGithubIdentifier =
                    MailAddress.TryCreate(expectedIdentifier, out var address)
                    && string.Equals(address.Address, expectedIdentifier,
                        StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                validGithubIdentifier = Regex.IsMatch(
                    expectedIdentifier,
                    @"^[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,37}[a-zA-Z0-9])?$");
            }

            if (!validGithubIdentifier)
                return BadRequest(new
                {
                    message = "Enter a GitHub username (not display name) or verified GitHub email."
                });
        }

        // Only accept local Vite origins used by our development app.
        string frontendOrigin = string.IsNullOrWhiteSpace(request.FrontendOrigin)
            ? "http://localhost:5173"
            : request.FrontendOrigin.Trim();

        if (!Uri.TryCreate(frontendOrigin, UriKind.Absolute, out var frontendUri)
            || frontendUri.Scheme != Uri.UriSchemeHttp
            || frontendUri.Host is not ("localhost" or "127.0.0.1")
            || frontendUri.Port < 5173 || frontendUri.Port > 5199
            || frontendUri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(frontendUri.Query)
            || !string.IsNullOrEmpty(frontendUri.Fragment)
            || frontendOrigin != frontendUri.GetLeftPart(UriPartial.Authority))
        {
            return BadRequest(new { message = "Invalid local frontend origin." });
        }

        // OAuth credentials from User Secrets
        string? clientId =
            _config[$"OAuth:{provider}:ClientId"];

        string? clientSecret =
            _config[$"OAuth:{provider}:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            return StatusCode(503, new
            {
                message =
                    $"OAuth credentials for {provider} are not configured."
            });
        }

        // Random state binds callback to Owner
        string state = RandomToken(32);

        // PKCE
        string codeVerifier = RandomToken(64);

        string challenge =
            WebEncoders.Base64UrlEncode(
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(codeVerifier)
                )
            );

        string baseUrl =
            _config["OAuth:CallbackBaseUrl"]?.TrimEnd('/')
            ?? "https://localhost:7015";

        if (!Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var callbackBase)
            || callbackBase.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(callbackBase.Query)
            || !string.IsNullOrEmpty(callbackBase.Fragment))
        {
            return StatusCode(503, new
            {
                message =
                    "Configure OAuth:CallbackBaseUrl as an HTTPS URL."
            });
        }

        string redirectUri =
            $"{baseUrl}/api/assets/connections/" +
            $"{provider.ToLowerInvariant()}/callback";

        var pending = new PendingAssetConnection
        {
            OwnerId =
                _currentUser.GetRequiredAccountId(),

            Provider = provider,

            Name = request.Name.Trim(),

            Description = request.Description,

            FileId = request.FileId?.Trim(),

            ExpectedIdentifier =
                needsIdentifier
                    ? expectedIdentifier
                    : null,

            RedirectUri = redirectUri,

            CodeVerifier = codeVerifier,

            FrontendOrigin = frontendOrigin
        };

        _cache.Set(
            StateKey(state),
            pending,
            SessionLifetime
        );

        var (authorizationEndpoint, _, scope) =
            Endpoints(provider);

        if (provider == "GITHUB" && expectedIdentifier.Contains('@'))
        {
            // Required to call GET /user/emails for verified email matching.
            scope = "read:user user:email";
        }

        var parameters =
            new Dictionary<string, string?>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = scope,
                ["state"] = state,
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256"
            };

        if (provider == "GMAIL")
        {
            // Force the account chooser so a saved browser session cannot
            // silently select another Google identity. Do not set login_hint:
            // Google documents that login_hint can suppress the chooser.
            parameters["prompt"] = "select_account";
        }

        if (provider == "GITHUB")
        {
            // Suggest the intended GitHub account; user still authorizes it.
            if (!expectedIdentifier.Contains('@'))
                parameters["login"] = expectedIdentifier;

            parameters["prompt"] = "select_account";
        }

        string authorizationUrl =
            QueryHelpers.AddQueryString(
                authorizationEndpoint,
                parameters
            );

        return Ok(new
        {
            authorizationUrl,
            expiresInSeconds = 600
        });
    }

    // ========================================
    // B6. OAUTH CALLBACK
    // ========================================

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        provider = provider.Trim().ToUpperInvariant();

        if (!IsSupported(provider)
            || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest("Invalid OAuth callback.");
        }

        if (!_cache.TryGetValue(
                StateKey(state),
                out PendingAssetConnection? pending)
            || pending is null
            || pending.Provider != provider)
        {
            return BadRequest(
                "OAuth state expired or invalid."
            );
        }

        // One-use state
        _cache.Remove(StateKey(state));

        if (!string.IsNullOrWhiteSpace(error))
        {
            return OAuthErrorPage(pending,
                "Authorization was cancelled or denied.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return OAuthErrorPage(pending,
                "Authorization code missing.");
        }

        try
        {
            // B6.1. Exchange authorization code
            string accessToken =
                await ExchangeCodeAsync(pending, code);

            // B6.2. Obtain provider identity
            var (
                externalId,
                displayName,
                accountId
            ) = await FetchIdentityAsync(
                pending,
                accessToken
            );

            if (string.IsNullOrWhiteSpace(externalId))
            {
                return OAuthErrorPage(pending,
                    "Provider returned no account/file identifier. Please retry verification.");
            }

            // B6.3. Check the real provider identity before issuing a ticket.
            bool matched = true;
            string declared = pending.ExpectedIdentifier?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(declared))
            {
                if (pending.Provider == "GITHUB" && declared.Contains('@'))
                {
                    // Email needs GitHub user:email scope and verified=true.
                    // Do not compare email to GitHub /user.login.
                    matched = await MatchesVerifiedGitHubEmailAsync(
                        accessToken, declared);
                }
                else
                {
                    // GitHub username or Google verified email.
                    matched = string.Equals(displayName?.Trim(), declared,
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!matched)
            {
                string message = pending.Provider == "GITHUB"
                    ? $"You authorized GitHub account @{displayName}, but it does not match the username or verified email entered in LegacyVault. Select the correct GitHub account or update the account field."
                    : $"You selected Google account {displayName}, but entered {declared} in LegacyVault. Select the matching Google account or correct the Gmail address.";

                return OAuthErrorPage(pending, message);
            }

            // B6.4. Issue short-lived completion ticket
            string ticket = RandomToken(32);

            _cache.Set(
                TicketKey(ticket),

                new VerifiedAssetConnection
                {
                    Pending = pending,

                    ExternalIdentifier =
                        externalId,

                    DisplayName = displayName,

                    AccountIdentifier =
                        accountId
                },

                SessionLifetime
            );

            // B6.5. Notify Frontend
            string safeTicket =
                JsonSerializer.Serialize(ticket);

            string safeOrigin =
                JsonSerializer.Serialize(pending.FrontendOrigin);

            string html = $$"""
                <!doctype html>
                <html>
                <head>
                    <meta charset="utf-8">
                    <title>LegacyVault OAuth</title>
                </head>
                <body>
                    <h3>Verification completed</h3>
                    <p>
                        Return to LegacyVault to finish
                        creating the asset.
                    </p>

                    <script>
                        if (window.opener) {
                            window.opener.postMessage(
                                {
                                    type: "LEGACYVAULT_OAUTH",
                                    ticket: {{safeTicket}}
                                },
                                {{safeOrigin}}
                            );
                            window.close();
                        }
                    </script>
                </body>
                </html>
                """;

            Response.Headers.CacheControl =
                "no-store";

            return Content(
                html,
                "text/html; charset=utf-8"
            );
        }
        catch (HttpRequestException)
        {
            return OAuthErrorPage(pending,
                "Could not contact the provider. Check your connection and OAuth settings.");
        }
        catch (InvalidOperationException ex)
        {
            return OAuthErrorPage(pending, ex.Message);
        }
        catch (JsonException)
        {
            return OAuthErrorPage(pending,
                "Unexpected response from the OAuth provider.");
        }
    }

    // ========================================
    // B7. COMPLETE AND SAVE ASSET
    // ========================================

    [HttpPost("complete")]
    [Authorize]
    public async Task<IActionResult> Complete(
        [FromBody] CompleteAssetConnectionRequest request)
    {
        if (
            string.IsNullOrWhiteSpace(request.Ticket)
            ||
            !_cache.TryGetValue(
                TicketKey(request.Ticket),
                out VerifiedAssetConnection? verified
            )
            ||
            verified is null
        )
        {
            return BadRequest(new
            {
                message =
                    "Ticket is invalid or expired."
            });
        }

        // Verify current Owner
        Guid ownerId =
            _currentUser.GetRequiredAccountId();

        if (verified.Pending.OwnerId != ownerId)
        {
            return Forbid();
        }

        // B7.1. Check duplicate verified Asset
        bool alreadyExists =
            await _db.AssetVerifications
                .AsNoTracking()
                .AnyAsync(v =>
                    v.Provider ==
                        verified.Pending.Provider

                    && v.ExternalIdentifier ==
                        verified.ExternalIdentifier

                    && _db.Assets.Any(a =>
                        a.Id == v.AssetId &&
                        a.OwnerAccountId == ownerId
                    )
                );

        if (alreadyExists)
        {
            _cache.Remove(
                TicketKey(request.Ticket)
            );

            return Conflict(new
            {
                message =
                    "This verified account or file " +
                    "is already in your vault."
            });
        }

        // B7.2. Validate handover details
        bool hasHandoverSecret =
            !string.IsNullOrEmpty(
                request.HandoverPassword
            );

        if (
            hasHandoverSecret
            &&
            verified.Pending.Provider is not
                ("GMAIL" or "GITHUB")
        )
        {
            return BadRequest(new
            {
                message =
                    "Only account assets support password handover."
            });
        }

        if (
            hasHandoverSecret
            &&
            !request.HandoverConsent
        )
        {
            return BadRequest(new
            {
                message =
                    "Handover information requires " +
                    "owner authorization confirmation."
            });
        }

        if (
            (request.HandoverPassword?.Length ?? 0) > 1024
        )
        {
            return BadRequest(new
            {
                message =
                    "Handover information exceeds allowed length."
            });
        }

        // B7.3. Prevent ticket replay
        lock (verified)
        {
            if (verified.Consumed)
            {
                return Conflict(new
                {
                    message = "Ticket already used."
                });
            }

            verified.Consumed = true;
        }

        _cache.Remove(
            TicketKey(request.Ticket)
        );

        // B7.4. Determine AssetType
        string assetType =
            verified.Pending.Provider == "GOOGLE_DRIVE"
                ? "CLOUD_STORAGE"
                : "ACCOUNT";

        // B7.5. Prepare SecretData
        var data =
            new Dictionary<string, string>
            {
                ["externalIdentifier"] =
                    verified.ExternalIdentifier,

                ["displayName"] =
                    verified.DisplayName,

                ["verificationMethod"] =
                    "OAUTH",

                ["verifiedAccountIdentifier"] =
                    verified.DisplayName
            };

        if (verified.AccountIdentifier != null)
        {
            data["accountIdentifier"] =
                verified.AccountIdentifier;
        }

        // Optional handover secret
        if (hasHandoverSecret)
        {
            data["handoverUsername"] =
                verified.DisplayName;

            data["handoverPassword"] =
                request.HandoverPassword ?? "";

            // OAuth does not verify this password
            data["handoverPasswordVerified"] =
                "false";
        }

        // B7.6. Save Asset and Verification
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var asset =
            await _assets.CreateAsync(
                new CreateAssetRequest
                {
                    Name =
                        verified.Pending.Name,

                    Description =
                        verified.Pending.Description,

                    AssetType = assetType,

                    Provider =
                        verified.Pending.Provider,

                    SecretData = data
                }
            );

        var trackedAsset =
            await _db.Assets.FindAsync(
                asset.Id
            );

        if (trackedAsset == null)
        {
            throw new InvalidOperationException(
                "Created asset not found."
            );
        }

        string status =
            verified.Pending.Provider switch
            {
                "GOOGLE_DRIVE" =>
                    "FILE_ACCESS_VERIFIED",

                _ =>
                    "PROVIDER_VERIFIED"
            };

        trackedAsset.VerificationStatus =
            status;

        trackedAsset.UpdatedAt =
            DateTimeOffset.UtcNow;

        _db.AssetVerifications.Add(
            new AssetVerification
            {
                Id = Guid.NewGuid(),

                AssetId = asset.Id,

                Provider =
                    verified.Pending.Provider,

                ExternalIdentifier =
                    verified.ExternalIdentifier,

                Status = status,

                VerifiedAt =
                    DateTimeOffset.UtcNow,

                CreatedAt =
                    DateTimeOffset.UtcNow
            }
        );

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return StatusCode(
            201,
            new
            {
                assetId = asset.Id,
                verificationStatus = status
            }
        );
    }

    // ========================================
    // B8. EXCHANGE OAUTH CODE
    // ========================================

    private async Task<string> ExchangeCodeAsync(
        PendingAssetConnection pending,
        string code)
    {
        var (_, tokenEndpoint, _) =
            Endpoints(pending.Provider);

        var body =
            new Dictionary<string, string>
            {
                ["client_id"] =
                    _config[
                        $"OAuth:{pending.Provider}:ClientId"
                    ]!,

                ["client_secret"] =
                    _config[
                        $"OAuth:{pending.Provider}:ClientSecret"
                    ]!,

                ["code"] = code,

                ["grant_type"] =
                    "authorization_code",

                ["redirect_uri"] =
                    pending.RedirectUri,

                ["code_verifier"] =
                    pending.CodeVerifier
            };

        using var req =
            new HttpRequestMessage(
                HttpMethod.Post,
                tokenEndpoint
            )
            {
                Content =
                    new FormUrlEncodedContent(body)
            };

        req.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"
            )
        );

        using var response =
            await _httpFactory
                .CreateClient("asset-oauth")
                .SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Token exchange failed " +
                $"({(int)response.StatusCode}). " +
                "Check OAuth callback and app settings."
            );
        }

        using var json =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync()
            );

        if (
            !json.RootElement.TryGetProperty(
                "access_token",
                out var token
            )
            ||
            string.IsNullOrWhiteSpace(
                token.GetString()
            )
        )
        {
            throw new InvalidOperationException(
                "Provider did not issue an access token."
            );
        }

        return token.GetString()!;
    }

    // ========================================
    // B9. FETCH VERIFIED IDENTITY
    // ========================================

    private async Task<
        (
            string id,
            string name,
            string? accountId
        )
    > FetchIdentityAsync(
        PendingAssetConnection pending,
        string token)
    {
        using var req =
            new HttpRequestMessage(
                HttpMethod.Get,

                pending.Provider switch
                {
                    "GMAIL" or "GOOGLE_DRIVE" =>
                        "https://openidconnect.googleapis.com/v1/userinfo",

                    "GITHUB" =>
                        "https://api.github.com/user",

                    _ =>
                        throw new InvalidOperationException(
                            "Unsupported provider."
                        )
                }
            );

        req.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        req.Headers.UserAgent.ParseAdd(
            "LegacyVault/1.0"
        );

        using var result =
            await _httpFactory
                .CreateClient("asset-oauth")
                .SendAsync(req);

        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Provider profile verification failed " +
                $"({(int)result.StatusCode})."
            );
        }

        using var json =
            JsonDocument.Parse(
                await result.Content.ReadAsStringAsync()
            );

        var root = json.RootElement;

        // Provider stable ID
        string id =
            pending.Provider switch
            {
                "GMAIL" or "GOOGLE_DRIVE" =>
                    ReadString(root, "sub"),

                "GITHUB" =>
                    root.GetProperty("id").ToString(),

                _ => ""
            };

        // Email / username
        string name =
            pending.Provider switch
            {
                "GMAIL" or "GOOGLE_DRIVE" =>
                    ReadString(root, "email"),

                "GITHUB" =>
                    ReadString(root, "login"),

                _ => id
            };

        // Google verified email
        if (pending.Provider == "GMAIL" && string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Google did not return an email address. Check OAuth scopes (openid email profile)."
            );
        }

        if (
            pending.Provider == "GMAIL"
            &&
            (
                !root.TryGetProperty(
                    "email_verified",
                    out var emailVerified
                )
                ||
                emailVerified.ValueKind !=
                    JsonValueKind.True
            )
        )
        {
            throw new InvalidOperationException(
                "Google has not verified this email address."
            );
        }

        // Google Drive file access
        if (pending.Provider == "GOOGLE_DRIVE")
        {
            string fileId = pending.FileId!;

            using var fileReq =
                new HttpRequestMessage(
                    HttpMethod.Get,

                    "https://www.googleapis.com/drive/v3/files/"
                    +
                    Uri.EscapeDataString(fileId)
                    +
                    "?fields=id,name,trashed"
                );

            fileReq.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token
                );

            using var fileResponse =
                await _httpFactory
                    .CreateClient("asset-oauth")
                    .SendAsync(fileReq);

            if (!fileResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Cannot access selected Google Drive file " +
                    "with granted permissions."
                );
            }

            using var fileJson =
                JsonDocument.Parse(
                    await fileResponse.Content.ReadAsStringAsync()
                );

            var file =
                fileJson.RootElement;

            if (
                file.TryGetProperty(
                    "trashed",
                    out var trashed
                )
                &&
                trashed.GetBoolean()
            )
            {
                throw new InvalidOperationException(
                    "Google Drive file is in trash."
                );
            }

            return (
                ReadString(file, "id"),
                ReadString(file, "name"),
                id
            );
        }

        return (id, name, null);
    }

    // ========================================
    // B10. HELPERS
    // ========================================

    // Compare only emails that GitHub confirms as verified and belonging
    // to the OAuth-authorized user. The public /user.email may be null.
    private async Task<bool> MatchesVerifiedGitHubEmailAsync(
        string accessToken, string expectedEmail)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "https://api.github.com/user/emails?per_page=100");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("LegacyVault/1.0");
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await _httpFactory
            .CreateClient("asset-oauth").SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Unable to confirm the GitHub email. Please authorize email access, " +
                "or enter your GitHub username instead.");
        }

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        if (json.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Unexpected GitHub email response.");

        foreach (JsonElement item in json.RootElement.EnumerateArray())
        {
            if (item.TryGetProperty("email", out var email)
                && email.ValueKind == JsonValueKind.String
                && item.TryGetProperty("verified", out var verified)
                && verified.ValueKind == JsonValueKind.True
                && string.Equals(email.GetString(), expectedEmail,
                    StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // Notify the verified local Frontend origin rather than leaving an
    // opaque error page in the OAuth popup. Never send access tokens.
    private IActionResult OAuthErrorPage(
        PendingAssetConnection pending, string errorMessage)
    {
        string safeOrigin = JsonSerializer.Serialize(pending.FrontendOrigin);
        string safeMessage = JsonSerializer.Serialize(errorMessage);

        string html = $$"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>LegacyVault OAuth</title></head>
            <body>
                <p>Account verification was not completed. Return to LegacyVault.</p>
                <script>
                    if (window.opener) {
                        window.opener.postMessage(
                            { type: "LEGACYVAULT_OAUTH_ERROR", message: {{safeMessage}} },
                            {{safeOrigin}}
                        );
                        window.close();
                    }
                </script>
            </body>
            </html>
            """;

        Response.Headers.CacheControl = "no-store";
        return Content(html, "text/html; charset=utf-8");
    }

    private static string ReadString(
        JsonElement root,
        string property)
    {
        return root.TryGetProperty(
            property,
            out var value
        )
            ? value.ToString()
            : "";
    }

    private static bool IsSupported(
        string provider)
    {
        return provider is
            "GMAIL"
            or "GITHUB"
            or "GOOGLE_DRIVE";
    }

    private static (
        string authorize,
        string token,
        string scope
    ) Endpoints(string provider)
    {
        return provider switch
        {
            "GMAIL" => (
                "https://accounts.google.com/o/oauth2/v2/auth",
                "https://oauth2.googleapis.com/token",
                "openid email profile"
            ),

            "GOOGLE_DRIVE" => (
                "https://accounts.google.com/o/oauth2/v2/auth",
                "https://oauth2.googleapis.com/token",
                "openid email https://www.googleapis.com/auth/drive.file"
            ),

            "GITHUB" => (
                "https://github.com/login/oauth/authorize",
                "https://github.com/login/oauth/access_token",
                "read:user"
            ),

            _ => throw new InvalidOperationException(
                "Unsupported provider."
            )
        };
    }

    private static string RandomToken(
        int bytes)
    {
        return WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(bytes)
        );
    }

    private static string StateKey(
        string state)
    {
        return "lv:oauth:state:" + state;
    }

    private static string TicketKey(
        string ticket)
    {
        return "lv:oauth:ticket:" + ticket;
    }
}
