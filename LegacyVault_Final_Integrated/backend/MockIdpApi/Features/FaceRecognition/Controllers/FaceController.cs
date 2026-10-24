using MockIdpApi.Features.FaceRecognition.Contracts;
using MockIdpApi.Features.FaceRecognition.DTOs;
using MockIdpApi.Features.FaceRecognition.Services;
using MockIdpApi.Features.IdentityAccess.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace MockIdpApi.Features.FaceRecognition.Controllers;

[ApiController]
[Route("api/idp-users/{idpUserId:guid}/face")]
//[Authorize]
public class IdpFaceController : ControllerBase
{
    private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB

    private readonly IFaceService _faceService;

    public IdpFaceController(IFaceService faceService)
    {
        _faceService = faceService;
    }

    [HttpPost("enroll")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Enroll(
        Guid idpUserId,
        [FromForm] FaceEnrollRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FaceImage is null || request.FaceImage.Length == 0)
            return BadRequest(new { message = "A face image is required." });

        if (request.FaceImage.Length > MaxImageSize)
            return BadRequest(new { message = "Image must not exceed 5 MB." });

        try
        {
            await using var stream = request.FaceImage.OpenReadStream();

            var result = await _faceService.EnrollAsync(
                idpUserId, stream, cancellationToken);

            return ToActionResult(result);
        }
        catch (InvalidFaceImageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidImageContentException)
        {
            return UnprocessableEntity(new { message = "The uploaded file is not a valid image." });
        }
    }

    [HttpPost("verify")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Verify(
        Guid idpUserId,
        [FromForm] FaceVerifyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FaceImage is null || request.FaceImage.Length == 0)
            return BadRequest(new { message = "A face image is required." });

        if (request.FaceImage.Length > MaxImageSize)
            return BadRequest(new { message = "Image must not exceed 5 MB." });

        try
        {
            await using var stream = request.FaceImage.OpenReadStream();

            var result = await _faceService.VerifyAsync(
                idpUserId, stream, cancellationToken);

            return ToActionResult(result);
        }
        catch (InvalidFaceImageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidImageContentException)
        {
            return UnprocessableEntity(new { message = "The uploaded file is not a valid image." });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        Guid idpUserId,
        CancellationToken cancellationToken)
    {
        var result = await _faceService.DeleteAsync(idpUserId, cancellationToken);
        return result.Status == ServiceStatus.Ok ? NoContent() : ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ServiceResult<T> result) =>
        result.Status switch
        {
            ServiceStatus.Ok       => Ok(result.Data),
            ServiceStatus.Created  => StatusCode(StatusCodes.Status201Created, result.Data),
            ServiceStatus.NotFound => NotFound(new { message = result.Error }),
            ServiceStatus.Conflict => Conflict(new { message = result.Error }),
            ServiceStatus.Forbidden=> StatusCode(StatusCodes.Status403Forbidden, new { message = result.Error }),
            _                      => BadRequest(new { message = result.Error })
        };
}