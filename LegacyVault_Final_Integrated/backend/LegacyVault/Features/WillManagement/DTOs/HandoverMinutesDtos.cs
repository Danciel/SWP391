namespace LegacyVault.Features.WillManagement.DTOs
{
    // One line of the minutes: one asset given to one beneficiary.
    public class HandoverMinutesLine
    {
        public Guid AssetAssignmentId { get; set; }

        public string AssetName { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public Guid BeneficiaryDesignationId { get; set; }

        public string BeneficiaryName { get; set; } = string.Empty;

        public string? Relationship { get; set; }

        public decimal AllocationPercentage { get; set; }
    }

    // The signed content. Property ORDER matters: it is serialized once and
    // the SHA-256 is taken over that exact JSON text.
    public class HandoverMinutesContent
    {
        public int Version { get; set; } = 1;

        public Guid HandoverCaseId { get; set; }

        public Guid WillId { get; set; }

        public string WillTitle { get; set; } = string.Empty;

        public string HandoverType { get; set; } = string.Empty;

        public DateTimeOffset StartedAt { get; set; }

        public Guid OwnerAccountId { get; set; }

        public string OwnerName { get; set; } = string.Empty;

        public Guid ExecutorAccountId { get; set; }

        public string ExecutorName { get; set; } = string.Empty;

        public List<HandoverMinutesLine> Items { get; set; } = new();
    }

    public class HandoverSignatureResponse
    {
        public Guid Id { get; set; }

        public string Role { get; set; } = string.Empty;

        public string SignerName { get; set; } = string.Empty;

        // Owner / Executor / Admin only. Null in a beneficiary's own view.
        public Guid? BeneficiaryDesignationId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset? SignedAt { get; set; }

        public string? SignatureHash { get; set; }
    }

    public class HandoverMinutesResponse
    {
        public Guid Id { get; set; }

        public Guid HandoverCaseId { get; set; }

        public Guid WillId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string ContentSha256 { get; set; } = string.Empty;

        public string PdfSha256 { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? FullySignedAt { get; set; }

        public HandoverMinutesContent Content { get; set; } = new();

        public List<HandoverSignatureResponse> Signatures { get; set; } = new();
    }

    // Small block added to the handover screen response.
    public class HandoverMinutesSummary
    {
        public string Status { get; set; } = string.Empty;

        public bool ExecutorSigned { get; set; }

        public int SignedCount { get; set; }

        public int TotalSignatures { get; set; }
    }
}
