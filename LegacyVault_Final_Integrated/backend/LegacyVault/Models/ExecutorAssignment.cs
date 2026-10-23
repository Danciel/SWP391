namespace LegacyVault.Models
{
    public class ExecutorAssignment
    {
        public Guid Id { get; set; }

        public Guid WillId { get; set; }

        public Guid ExecutorAccountId { get; set; }

        public string Status { get; set; } = "PENDING";

        public DateTimeOffset AssignedAt { get; set; }

        public DateTimeOffset? RespondedAt { get; set; }

        // Immutable fee snapshot captured when the Owner selects this Executor.
        public decimal ServiceFeeSnapshot { get; set; }

        public string CurrencySnapshot { get; set; } = "VND";

        // This assignment is scoped to the entire Will, not an individual asset.
        public string Scope { get; set; } = "WILL";

        public Will? Will { get; set; }

        public Account? ExecutorAccount { get; set; }
    }
}
