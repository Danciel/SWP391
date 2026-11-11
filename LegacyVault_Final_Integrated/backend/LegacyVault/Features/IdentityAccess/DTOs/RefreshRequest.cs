using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class RefreshRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}