using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MockIdpApi.Features.FaceRecognition.DTOs;

public class FaceEnrollRequest
{

    [Required]
    public IFormFile FaceImage { get; set; } = default!;
}
