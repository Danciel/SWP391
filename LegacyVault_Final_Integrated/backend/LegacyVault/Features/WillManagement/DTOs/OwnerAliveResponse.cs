namespace LegacyVault.Features.WillManagement.DTOs
{
    public class OwnerAliveResponse
    {
        public Guid WillId { get; set; }

        public string DmsStatus { get; set; } = string.Empty;

        public DateTimeOffset? NextCheckInAt { get; set; }

        // Pending or approved legal cases that were cancelled.
        public int CancelledLegalCases { get; set; }

        public bool ExecutorNotified { get; set; }
    }
}
