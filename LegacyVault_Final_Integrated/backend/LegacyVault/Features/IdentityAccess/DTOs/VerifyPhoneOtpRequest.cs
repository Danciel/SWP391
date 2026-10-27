using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class VerifyPhoneOtpRequest
        {
            [Required]
            [RegularExpression(@"^\d{6}$")]
            public string Otp { get; set; } = string.Empty;
    }
}