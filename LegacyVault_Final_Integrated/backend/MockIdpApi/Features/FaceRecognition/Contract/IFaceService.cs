using MockIdpApi.Features.FaceRecognition.DTOs;
using MockIdpApi.Features.IdentityAccess.Contracts;

namespace MockIdpApi.Features.FaceRecognition.Contracts;

public interface IFaceService
{
    Task<ServiceResult<FaceEnrollResponse>> EnrollAsync(
        Guid idpUserId,
        Stream imageStream,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<FaceVerifyResponse>> VerifyAsync(
        Guid idpUserId,
        Stream imageStream,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAsync(
        Guid idpUserId,
        CancellationToken cancellationToken = default);
}