using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class AdminCreateAccountRequest
    {
        [Required, StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(32)]
        public string? PhoneNumber { get; set; }

        [Required, MinLength(8), StringLength(128)]
        public string Password { get; set; } = string.Empty;

        // "USER" or "ADMIN"
        [StringLength(32)]
        public string Role { get; set; } = "USER";

        public bool EmailVerified { get; set; }

        public bool PhoneVerified { get; set; }
    }
}
