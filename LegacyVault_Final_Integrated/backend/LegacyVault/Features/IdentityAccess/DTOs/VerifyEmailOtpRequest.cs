using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class VerifyEmailOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$")]
        public string Otp { get; set; } = string.Empty;

        public string Purpose { get; set; } = "register";
    }
}