using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using LegacyVault.Features.IdentityAccess.Contracts;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class IdpIntegrationService : IIdpIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdpIntegrationService> _logger;

        public IdpIntegrationService(HttpClient httpClient, IConfiguration config, ILogger<IdpIntegrationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Point this to your MockIdpApi port (e.g., https://localhost:7001/)
            _httpClient.BaseAddress = new Uri(config["IdpApiSettings:BaseUrl"] ?? "https://localhost:7001/");
        }

        public async Task<bool> VerifyFaceAsync(Guid idpUserId, Stream faceImageStream, CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(faceImageStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "FaceImage", "face.jpg");

                // Call the MockIdpApi verify endpoint
                var response = await _httpClient.PostAsync($"api/idp-users/{idpUserId}/face/verify", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"IDP Face Verify failed with status {response.StatusCode}");
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

        private class FaceVerifyResponse { public bool Matched { get; set; } public double Score { get; set; } }
    }
}