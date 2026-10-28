using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    // Fields shared by every place that shows the system suggestion.
    public class FeeSuggestionFields
    {
        // Null when the Executor has not set a fee or the currency differs from the rule currency.
        public decimal? SuggestedFee { get; set; }
        // (asking - suggested) / suggested * 100, one decimal. Null when the fee is not set.
        public decimal? FeeDifferencePercent { get; set; }
        // ABOVE | BELOW | AT (within 5%) | null
        public string? FeePosition { get; set; }
        // CSV | PROFILE
        public string EvaluationSource { get; set; } = "PROFILE";
        public DateTimeOffset? EvaluationUpdatedAt { get; set; }
        public int? EvalExperienceYears { get; set; }
        public decimal? EvalRating { get; set; }
        public int? TotalCases { get; set; }
        public int? WonCases { get; set; }
        public decimal? WinRatePercent { get; set; }
    }

    public class SuggestionComponent
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    // Executor's own view: how the suggested price was built.
    public class ExecutorPricingSuggestionResponse : FeeSuggestionFields
    {
        public string Currency { get; set; } = "VND";
        public decimal CurrentFee { get; set; }
        public List<SuggestionComponent> Components { get; set; } = new();
        public decimal MinFee { get; set; }
        public decimal MaxFee { get; set; }
        public bool Clamped { get; set; }
    }

    public class ExecutorPricingRuleResponse
    {
        public decimal BaseFee { get; set; }
        public decimal PerExperienceYear { get; set; }
        public decimal PerStar { get; set; }
        public decimal PerCompletedCase { get; set; }
        public decimal WinRateBonus { get; set; }
        public decimal MinFee { get; set; }
        public decimal MaxFee { get; set; }
        public decimal RoundTo { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDefault { get; set; }
    }

    public class UpdateExecutorPricingRuleRequest
    {
        [Range(0, 1_000_000_000)] public decimal BaseFee { get; set; }
        [Range(0, 1_000_000_000)] public decimal PerExperienceYear { get; set; }
        [Range(0, 1_000_000_000)] public decimal PerStar { get; set; }
        [Range(0, 1_000_000_000)] public decimal PerCompletedCase { get; set; }
        [Range(0, 1_000_000_000)] public decimal WinRateBonus { get; set; }
        [Range(0, 1_000_000_000)] public decimal MinFee { get; set; }
        [Range(0, 1_000_000_000)] public decimal MaxFee { get; set; }
        [Range(0, 1_000_000_000)] public decimal RoundTo { get; set; }
        [Required, StringLength(3, MinimumLength = 3)] public string Currency { get; set; } = "VND";
    }
}
