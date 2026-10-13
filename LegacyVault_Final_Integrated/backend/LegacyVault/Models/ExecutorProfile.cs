namespace LegacyVault.Models
{
    public class ExecutorProfile
    {
        public Guid AccountId { get; set; }

        public decimal Rating { get; set; }

        public int CompletedCases { get; set; }

        public int ExperienceYears { get; set; }

        public string? Bio { get; set; }

        public bool IsVerified { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Account? Account { get; set; }
    }
}
