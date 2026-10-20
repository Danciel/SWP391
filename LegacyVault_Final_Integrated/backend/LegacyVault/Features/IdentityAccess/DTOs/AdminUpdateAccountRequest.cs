using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    // All fields optional: only the fields that are sent are changed.
    public class AdminUpdateAccountRequest
    {
        [StringLength(200)]
        public string? FullName { get; set; }

        [EmailAddress, StringLength(256)]
        public string? Email { get; set; }

        [StringLength(32)]
        public string? PhoneNumber { get; set; }

        // Admin password reset
        [MinLength(8), StringLength(128)]
        public string? Password { get; set; }

        [StringLength(32)]
        public string? Role { get; set; }

        public bool? EmailVerified { get; set; }

        public bool? PhoneVerified { get; set; }

        [StringLength(32)]
        public string? IdentityStatus { get; set; }
    }
}
