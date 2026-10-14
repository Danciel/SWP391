using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class ResendEmailOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Purpose { get; set; } = "register";
    }
}