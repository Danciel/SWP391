using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class AdminCreateIdpUserRequest
    {
        [Required, StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(32)]
        public string? PhoneNumber { get; set; }

        // e.g. UNVERIFIED, PENDING, VERIFIED, REJECTED
        [StringLength(32)]
        public string IdentityStatus { get; set; } = "UNVERIFIED";

        // FaceId is intentionally omitted here; it is managed by the FaceId service.
    }
}