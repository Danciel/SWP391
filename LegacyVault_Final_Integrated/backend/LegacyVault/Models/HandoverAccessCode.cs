namespace LegacyVault.Models
{
    // One row per (handover case, recipient). Only a salted hash of the
    // access code is stored: the code itself exists only in the email.
    public class HandoverAccessCode
    {
        public Guid Id { get; set; }

        public Guid HandoverCaseId { get; set; }

        public Guid BeneficiaryDesignationId { get; set; }

        public byte[] Salt { get; set; } = Array.Empty<byte>();

        public byte[] CodeHash { get; set; } = Array.Empty<byte>();

        public DateTimeOffset IssuedAt { get; set; }

        public DateTimeOffset LastIssuedAt { get; set; }

        // How many codes were issued (first send + "send again").
        public int IssueCount { get; set; } = 1;

        public DateTimeOffset? EmailedAt { get; set; }

        public int FailedAttempts { get; set; }

        public DateTimeOffset? LockedUntil { get; set; }

        // Set when the recipient typed the right code; the claim needs it.
        public DateTimeOffset? VerifiedAt { get; set; }
    }
}
