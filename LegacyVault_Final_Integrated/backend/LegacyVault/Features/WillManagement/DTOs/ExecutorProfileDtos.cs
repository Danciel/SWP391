using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class UpdateExecutorProfileRequest
    {
        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(200)]
        public string? Specialty { get; set; }

        [Range(0, 80)]
        public int ExperienceYears { get; set; }
    }

    public class CreateExecutorReviewRequest
    {
        [Required]
        public Guid WillId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class ExecutorReviewResponse
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    // E10: reference price. Built only from data stored in this platform
    // (advertised fees + completed mock payments). It is NOT a market price.
    public class ExecutorPricingReferenceResponse
    {
        public string Currency { get; set; } = "VND";
        public string Source { get; set; } = string.Empty;
        public string Disclaimer { get; set; } = string.Empty;
        public bool HasReferenceData { get; set; }
        public int MinimumSampleSize { get; set; }
        public int SampleSize { get; set; }
        public decimal? Minimum { get; set; }
        public decimal? Median { get; set; }
        public decimal? Average { get; set; }
        public decimal? Maximum { get; set; }
        public int CompletedPaymentCount { get; set; }
        public decimal? CompletedPaymentAverage { get; set; }
        public List<ExecutorPricingComparisonItem> Executors { get; set; } = new();
    }

    public class ExecutorPricingComparisonItem
    {
        public Guid AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public decimal ServiceFee { get; set; }
        public decimal Rating { get; set; }
        public int CompletedCases { get; set; }
        // Null when there is not enough data to compute a reference.
        public decimal? DifferenceFromMedianPercent { get; set; }
    }

    public class ExecutorProfileResponse : FeeSuggestionFields
    {
        public Guid AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Specialty { get; set; }
        public int ExperienceYears { get; set; }
        public int CompletedCases { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public decimal ServiceFee { get; set; }
        public string Currency { get; set; } = "VND";
        public bool IsVerified { get; set; }
        public bool IsSuspended { get; set; }
        public List<ExecutorReviewResponse> RecentReviews { get; set; } = new();
    }
}
