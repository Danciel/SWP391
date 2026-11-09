using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class LinkIdpAccountRequest
    {
        [Required]
        public string ExternalIdentityId { get; set; } = string.Empty; // The IDP GUID

        [Required]
        public IFormFile FaceImage { get; set; } = default!;
    }
}