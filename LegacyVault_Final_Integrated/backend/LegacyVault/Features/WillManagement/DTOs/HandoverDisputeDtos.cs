using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class HandoverReasonRequest
    {
        [Required, StringLength(500, MinimumLength = 3)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReassignExecutorRequest
    {
        [Required]
        public Guid NewExecutorAccountId { get; set; }

        [Required, StringLength(500, MinimumLength = 3)]
        public string Reason { get; set; } = string.Empty;
    }

    // H06: result of an admin reassigning the executor of a will.
    public class ExecutorReassignmentResponse
    {
        public Guid WillId { get; set; }

        public string WillStatus { get; set; } = string.Empty;

        public Guid ExecutorAccountId { get; set; }

        public string ExecutorName { get; set; } = string.Empty;

        public string AssignmentStatus { get; set; } = string.Empty;

        public decimal ServiceFeeSnapshot { get; set; }

        public string CurrencySnapshot { get; set; } = "VND";
    }
}
