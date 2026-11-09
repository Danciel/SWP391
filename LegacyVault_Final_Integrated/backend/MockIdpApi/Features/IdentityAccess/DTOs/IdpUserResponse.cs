namespace MockIdpApi.Features.IdentityAccess.DTOs
{
    public class IdpUserResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string IdentityStatus { get; set; } = string.Empty;

        public string LifeStatus { get; set; } = string.Empty;

        // Managed by a separate service, exposed read-only here
        public Guid? FaceId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}