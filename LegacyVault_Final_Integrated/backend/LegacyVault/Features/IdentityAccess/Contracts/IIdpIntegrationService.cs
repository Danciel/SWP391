namespace LegacyVault.Features.IdentityAccess.Contracts
{
    /// <summary>
    /// Result of a call to the Identity Provider.
    /// StatusCode is the HTTP status the API should answer with;
    /// Message is safe to show to the user (it says WHY it failed).
    /// </summary>
    public sealed record IdpFaceResult(bool Success, int StatusCode, string Message)
    {
        public static IdpFaceResult Ok(string message) => new(true, 200, message);

        public static IdpFaceResult Fail(int statusCode, string message) => new(false, statusCode, message);
    }

    public interface IIdpIntegrationService
    {
        /// <summary>
        /// Checks a face photo against the face enrolled for an IdP profile.
        /// </summary>
        Task<IdpFaceResult> VerifyFaceAsync(Guid idpUserId, Stream faceImageStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the face, then links the LegacyVault account to the IdP profile
        /// (ExternalIdentityId + IdentityStatus = VERIFIED).
        /// </summary>
        Task<IdpFaceResult> LinkIdpUserAsync(Guid legacyVaultUserId, string externalIdentityId, Stream faceImageStream, CancellationToken cancellationToken = default);
    }
}
