using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LegacyVault.Features.IdentityAccess.Contracts;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class IdpIntegrationService : IIdpIntegrationService
    {
        // Same default as the other IdP client (IdpLifeStatusClient in Program.cs).
        // Plain HTTP on the MockIdpApi "http" profile: works without a dev HTTPS certificate.
        private const string DefaultBaseUrl = "http://localhost:5100/";

        private readonly HttpClient _httpClient;
        private readonly ILogger<IdpIntegrationService> _logger;
        private readonly IAccountRepository _accountRepository;

        public IdpIntegrationService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<IdpIntegrationService> logger,
            IAccountRepository accountRepository)
        {
            _httpClient = httpClient;
            _logger = logger;
            _accountRepository = accountRepository;

            // IdpApiSettings:BaseUrl wins; "Idp:BaseUrl" (used by IdpLifeStatusClient) is the fallback,
            // so one setting is enough to point LegacyVault at MockIdpApi.
            var baseUrl = config["IdpApiSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = config["Idp:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = DefaultBaseUrl;

            // Trailing "/" keeps relative paths ("api/idp-users/...") resolving correctly.
            _httpClient.BaseAddress = new Uri(baseUrl.Trim().TrimEnd('/') + "/");

            // First call loads the face models; do not let the UI hang forever if the IdP is stuck.
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<IdpFaceResult> VerifyFaceAsync(Guid idpUserId, Stream faceImageStream, CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy to memory stream to avoid stream disposed/read issues
                using var memoryStream = new MemoryStream();
                await faceImageStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(memoryStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "FaceImage", "face.jpg");

                using var response = await _httpClient.PostAsync($"api/idp-users/{idpUserId}/face/verify", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("IDP face verify failed with status {Status}. Response: {Body}", (int)response.StatusCode, errorBody);

                    return DescribeFailure(response.StatusCode, ReadMessage(errorBody));
                }

                var result = await response.Content.ReadFromJsonAsync<FaceVerifyResponse>(cancellationToken: cancellationToken);

                if (result?.Matched != true)
                {
                    _logger.LogInformation("IDP face verify: no match for {IdpUserId} (score {Score}).", idpUserId, result?.Score);

                    return IdpFaceResult.Fail(400,
                        $"The face does not match the enrolled profile (score {result?.Score:0.00}). Try again in better light, or re-enroll.");
                }

                return IdpFaceResult.Ok("Matched.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach the IDP API at {BaseAddress}.", _httpClient.BaseAddress);

                return IdpFaceResult.Fail(502,
                    $"Could not reach the Identity Provider at {_httpClient.BaseAddress}. Is MockIdpApi running, and is IdpApiSettings:BaseUrl correct?");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timed out calling the IDP API at {BaseAddress}.", _httpClient.BaseAddress);

                return IdpFaceResult.Fail(502, "The Identity Provider took too long to respond. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling IDP API for face verification.");

                return IdpFaceResult.Fail(500, "Unexpected error while verifying the face. Check the LegacyVault log.");
            }
        }

        public async Task<IdpFaceResult> LinkIdpUserAsync(Guid legacyVaultUserId, string externalIdentityId, Stream faceImageStream, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(externalIdentityId?.Trim(), out var idpUserId))
                return IdpFaceResult.Fail(400, "The IDP User ID is not a valid UUID."); // Invalid format

            // 1. Verify with Mock IDP
            var verification = await VerifyFaceAsync(idpUserId, faceImageStream, cancellationToken);
            if (!verification.Success) return verification;

            // 2. Fetch Account from LegacyVault DB using the Repository
            var account = await _accountRepository.GetByIdAsync(legacyVaultUserId);
            if (account == null)
                return IdpFaceResult.Fail(404, "Your account was not found.");

            try
            {
                // 3. Update the Account record
                account.ExternalIdentityId = idpUserId.ToString();
                account.IdentityStatus = "VERIFIED";
                account.UpdatedAt = DateTimeOffset.UtcNow;

                // 4. Save Changes using the Repository
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face matched but saving the identity link failed for account {AccountId}.", legacyVaultUserId);

                return IdpFaceResult.Fail(500, "Face matched, but the identity link could not be saved. Check the LegacyVault log.");
            }

            return IdpFaceResult.Ok("Identity linked successfully.");
        }

        // Maps the MockIdpApi error status to a message the user can act on.
        private static IdpFaceResult DescribeFailure(HttpStatusCode status, string? idpMessage) => status switch
        {
            HttpStatusCode.NotFound => IdpFaceResult.Fail(404,
                "IDP profile not found. Check the IDP User ID."),

            HttpStatusCode.Conflict => IdpFaceResult.Fail(400,
                "This IDP profile has no enrolled face yet. Enroll a face first."),

            HttpStatusCode.UnprocessableEntity => IdpFaceResult.Fail(400,
                $"{idpMessage ?? "The photo could not be used."} Retake it with only your own face in the frame."),

            _ => IdpFaceResult.Fail(502,
                $"The Identity Provider returned an error ({(int)status}).")
        };

        // MockIdpApi errors look like { "message": "..." }.
        private static string? ReadMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;

            try
            {
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString();
                }
            }
            catch (JsonException)
            {
                // Not JSON (e.g. an HTML error page).
            }

            return null;
        }

        private class FaceVerifyResponse { public bool Matched { get; set; } public double Score { get; set; } }
    }
}
