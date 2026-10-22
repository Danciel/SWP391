namespace LegacyVault.Models
{
    public class ExecutorProfile
    {
        public Guid AccountId { get; set; }

        public decimal Rating { get; set; }

        public int CompletedCases { get; set; }

        public int ExperienceYears { get; set; }

        public string? Bio { get; set; }

        // Executor's current advertised fee for handling one whole Will.
        public decimal ServiceFee { get; set; } = 0m;

        public string Currency { get; set; } = "VND";

        public bool IsVerified { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Account? Account { get; set; }
    }
}
