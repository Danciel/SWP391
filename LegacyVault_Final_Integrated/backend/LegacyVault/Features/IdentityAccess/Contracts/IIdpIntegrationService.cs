using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IIdpIntegrationService
    {
        Task<bool> VerifyFaceAsync(Guid idpUserId, Stream faceImageStream, CancellationToken cancellationToken = default);
    }
}