namespace LegacyVault.Models
{
    // One configurable row that drives the system's suggested Executor fee.
    //   suggested = BaseFee
    //             + PerExperienceYear * years   (capped at 40 years)
    //             + PerStar           * rating  (0..5)
    //             + PerCompletedCase  * cases   (capped at 200 cases)
    //             + WinRateBonus      * winRate (0..1, only when wins are known)
    // then clamped to [MinFee, MaxFee] and rounded to RoundTo.
    public class ExecutorPricingRule
    {
        public Guid Id { get; set; }
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
        public Guid? UpdatedByAccountId { get; set; }

        public static ExecutorPricingRule Default() => new()
        {
            Id = Guid.Empty,
            BaseFee = 10_000_000m,
            PerExperienceYear = 500_000m,
            PerStar = 300_000m,
            PerCompletedCase = 200_000m,
            WinRateBonus = 1_000_000m,
            MinFee = 10_000_000m,
            MaxFee = 30_000_000m,
            RoundTo = 10_000m,
            Currency = "VND",
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
