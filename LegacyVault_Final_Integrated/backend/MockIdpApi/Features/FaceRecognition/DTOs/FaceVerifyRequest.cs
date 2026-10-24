
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MockIdpApi.Features.FaceRecognition.DTOs;

public class FaceVerifyRequest
{

    [Required]
    public IFormFile FaceImage { get; set; } = default!;
}
