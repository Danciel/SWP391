namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class IdpUserSearchResponse
    {
        // Used to link with other entities later (e.g. ExternalIdentityId)
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        // Masked for privacy
        public string? MaskedEmail { get; set; }

        public string? MaskedPhoneNumber { get; set; }

        public string IdentityStatus { get; set; } = string.Empty;
    }
}