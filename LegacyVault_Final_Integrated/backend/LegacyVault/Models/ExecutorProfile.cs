namespace LegacyVault.Models
{
    public class ExecutorProfile
    {
        public Guid AccountId { get; set; }

        public decimal Rating { get; set; }

        public int CompletedCases { get; set; }

        public int ExperienceYears { get; set; }

        public string? Bio { get; set; }

        // LV-20: area of expertise shown to Owners.
        public string? Specialty { get; set; }

        // Executor's current advertised fee for handling one whole Will.
        public decimal ServiceFee { get; set; } = 0m;

        // Aggregate of all per-Will executor deposits currently held.
        public decimal TotalHeldDeposit { get; set; } = 0m;

        // Evaluation numbers read from the Executor's uploaded CSV.
        // They are self-reported, so the UI labels them with EvalSource.
        public int? EvalExperienceYears { get; set; }
        public decimal? EvalRating { get; set; }
        public int? EvalTotalCases { get; set; }
        public int? EvalWonCases { get; set; }
        public string? EvalSource { get; set; } // "CSV" | null
        public DateTimeOffset? EvalUpdatedAt { get; set; }

        public string Currency { get; set; } = "VND";

        public bool IsVerified { get; set; }

        // Admin oversight: suspended executors cannot receive new Wills or accept pending assignments.
        public bool IsSuspended { get; set; }
        public string? SuspensionReason { get; set; }
        public DateTimeOffset? SuspendedAt { get; set; }
        public Guid? SuspendedByAccountId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Account? Account { get; set; }
    }
}
