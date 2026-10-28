using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class HandoverItemResponse
    {
        public Guid Id { get; set; }
        public Guid AssetAssignmentId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string BeneficiaryName { get; set; } = string.Empty;
        public string? Relationship { get; set; }
        public decimal AllocationPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? DeliveredAt { get; set; }
    }

    public class HandoverResponse
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public int TotalItems { get; set; }
        public int DeliveredItems { get; set; }
        public List<HandoverItemResponse> Items { get; set; } = new();
    }

    public class LegalCaseSummaryResponse
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public string WillTitle { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string ExecutorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset SubmittedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
    }

    public class LegalCaseDetailResponse : LegalCaseSummaryResponse
    {
        public string OriginalFileName { get; set; } = string.Empty;
        // JSON typed by the Executor on the certificate form (may be null).
        public string? CertificateData { get; set; }
        public string? OwnerEmail { get; set; }
        public int AssetCount { get; set; }
        public int BeneficiaryCount { get; set; }
        // Read-only value from the third-party identity app (R10).
        public string LifeStatus { get; set; } = "UNKNOWN";
        public int MissedCheckIns { get; set; }
        public int MaxMissedCheckIns { get; set; }
        public DateTimeOffset? DmsThresholdReachedAt { get; set; }
    }

    public class LegalDecisionRequest
    {
        public bool Approve { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }
    }

    public class AdminDmsRequest
    {
        public DateTimeOffset? NextCheckInAt { get; set; }

        [Range(0, 100)]
        public int? MissedCheckIns { get; set; }

        [Range(1, 3650)]
        public int? CheckInIntervalDays { get; set; }

        [Range(0, 30)]
        public int? GraceDays { get; set; }

        // ACTIVE or THRESHOLD_REACHED
        public string? Status { get; set; }
    }

    public class AdminEvaluateRequest
    {
        // Simulated "current time" for demos. Null = real time.
        public DateTimeOffset? Now { get; set; }
    }
}
