using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LegacyVault.Features.IdentityAccess.Contracts; // Ensure this is here

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class IdpIntegrationService : IIdpIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdpIntegrationService> _logger;
        private readonly IAccountRepository _accountRepository; // Use your repository

        public IdpIntegrationService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<IdpIntegrationService> logger,
            IAccountRepository accountRepository) // Inject Repository instead of DbContext
        {
            _httpClient = httpClient;
            _logger = logger;
            _accountRepository = accountRepository;

            _httpClient.BaseAddress = new Uri(config["IdpApiSettings:BaseUrl"] ?? "https://localhost:7176/");
        }

        public async Task<bool> VerifyFaceAsync(Guid idpUserId, Stream faceImageStream, CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy to memory stream to avoid stream disposed/read issues
                using var memoryStream = new MemoryStream();
                await faceImageStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(memoryStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "FaceImage", "face.jpg");

                var response = await _httpClient.PostAsync($"api/idp-users/{idpUserId}/face/verify", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning($"IDP Face Verify failed with status {response.StatusCode}. Response: {errorBody}");
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<FaceVerifyResponse>(cancellationToken: cancellationToken);
                return result?.Matched ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling IDP API for face verification.");
                return false;
            }
        }

        public async Task<bool> LinkIdpUserAsync(Guid legacyVaultUserId, string externalIdentityId, Stream faceImageStream, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(externalIdentityId, out var idpUserId))
                return false; // Invalid format

            // 1. Verify with Mock IDP
            var isMatched = await VerifyFaceAsync(idpUserId, faceImageStream, cancellationToken);
            if (!isMatched) return false;

            // 2. Fetch Account from LegacyVault DB using your Repository
            var account = await _accountRepository.GetByIdAsync(legacyVaultUserId);
            if (account == null) return false;

            // 3. Update the Account record
            account.ExternalIdentityId = externalIdentityId;
            account.IdentityStatus = "VERIFIED"; // Or use your Enum
            account.UpdatedAt = DateTimeOffset.UtcNow;

            // 4. Save Changes using your Repository
            await _accountRepository.UpdateAsync(account, cancellationToken);

            return true;
        }

        private class FaceVerifyResponse { public bool Matched { get; set; } public double Score { get; set; } }
    }
}