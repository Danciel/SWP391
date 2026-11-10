using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    // C7
    public class ConfirmRecipientLinkRequest
    {
        [Required, StringLength(500, MinimumLength = 5)]
        public string Note { get; set; } = string.Empty;
    }

    // C9: one row of the Executor's "my handovers" screen.
    public class ExecutorHandoverSummaryResponse
    {
        public Guid WillId { get; set; }

        public string WillTitle { get; set; } = string.Empty;

        public string OwnerName { get; set; } = string.Empty;

        public string WillStatus { get; set; } = string.Empty;

        public string? DmsStatus { get; set; }

        // PENDING | APPROVED | REJECTED | CANCELLED (latest), or null.
        public string? LegalCaseStatus { get; set; }

        // IN_PROGRESS | PAUSED | COMPLETED (latest, aborted ones are ignored), or null.
        public string? HandoverStatus { get; set; }

        public string? HandoverType { get; set; }

        public int TotalItems { get; set; }

        public int DeliveredItems { get; set; }

        public HandoverMinutesSummary? Minutes { get; set; }

        // What the Executor should do next:
        // SUBMIT_DEATH_CERTIFICATE | WAIT_LEGAL_REVIEW | START_HANDOVER |
        // SIGN_MINUTES | DELIVER_ASSETS | WAIT_BENEFICIARY_SIGNATURES |
        // PAUSED | DONE
        public string NextStep { get; set; } = string.Empty;
    }
}
