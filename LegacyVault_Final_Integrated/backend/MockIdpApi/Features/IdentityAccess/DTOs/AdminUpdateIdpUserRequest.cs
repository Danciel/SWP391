using System.ComponentModel.DataAnnotations;

namespace MockIdpApi.Features.IdentityAccess.DTOs
{
    // All fields optional: only sent fields are changed.
    public class AdminUpdateIdpUserRequest
    {
        [StringLength(200)]
        public string? FullName { get; set; }

        [EmailAddress, StringLength(256)]
        public string? Email { get; set; }

        [StringLength(32)]
        public string? PhoneNumber { get; set; }

        [StringLength(32)]
        public string? IdentityStatus { get; set; }

        [StringLength(32)]
        public string? LifeStatus { get; set; }

        // FaceId is intentionally omitted; it is managed by the FaceId service.
    }
}