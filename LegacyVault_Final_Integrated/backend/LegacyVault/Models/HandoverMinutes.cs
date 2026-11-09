namespace LegacyVault.Models
{
    // C1: official minutes (bien ban) of one handover case.
    // The content never changes after creation; ContentSha256 proves it.
    // PdfContent is the PDF of the minutes (re-rendered once, with the
    // signatures, when the last person signs: that is the "sealed" copy).
    public class HandoverMinutes
    {
        public Guid Id { get; set; }

        public Guid HandoverCaseId { get; set; }

        public Guid WillId { get; set; }

        public int Version { get; set; } = 1;

        // PENDING_SIGNATURES | FULLY_SIGNED | VOID (handover aborted)
        public string Status { get; set; } = "PENDING_SIGNATURES";

        // Canonical JSON of the minutes (see HandoverMinutesContent).
        public string ContentJson { get; set; } = string.Empty;

        // SHA-256 (lower-case hex) of the UTF-8 bytes of ContentJson.
        public string ContentSha256 { get; set; } = string.Empty;

        public byte[] PdfContent { get; set; } = Array.Empty<byte>();

        // SHA-256 (lower-case hex) of PdfContent.
        public string PdfSha256 { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? FullySignedAt { get; set; }

        public HandoverCase? HandoverCase { get; set; }

        public ICollection<HandoverSignature> Signatures { get; set; }
            = new List<HandoverSignature>();
    }

    // One required signature: the Executor, or one Beneficiary designation.
    public class HandoverSignature
    {
        public Guid Id { get; set; }

        public Guid MinutesId { get; set; }

        public string Role { get; set; } = "EXECUTOR"; // EXECUTOR | BENEFICIARY

        // Null for the Executor row.
        public Guid? BeneficiaryDesignationId { get; set; }

        // Who is expected to sign (a beneficiary may not have an account yet).
        public Guid? SignerAccountId { get; set; }

        public string SignerName { get; set; } = string.Empty;

        public string Status { get; set; } = "PENDING"; // PENDING | SIGNED

        public DateTimeOffset? SignedAt { get; set; }

        public Guid? SignedByAccountId { get; set; }

        // ContentSha256 of the minutes at the moment of signing.
        public string? SignedContentSha256 { get; set; }

        // SHA-256(contentSha256|role|signer|designation|signedAt)
        public string? SignatureHash { get; set; }

        public string? IpAddress { get; set; }

        public HandoverMinutes? Minutes { get; set; }
    }
}
