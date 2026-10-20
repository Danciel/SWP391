using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.FaceRecognition.DTOs;

public class FaceEnrollRequest
{
    [Required]
    public string ExternalIdentityId { get; set; } = string.Empty;

    [Required]
    public IFormFile FaceImage { get; set; } = default!;
}
