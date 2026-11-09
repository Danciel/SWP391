namespace LegacyVault.Models
{
    // LV-32/33: one handover run of a Will.
    public class HandoverCase
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public string Type { get; set; } = "IMMEDIATE"; // IMMEDIATE | DEATH
        public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS | COMPLETED | ABORTED
        public Guid InitiatedByAccountId { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public Will? Will { get; set; }
        public ICollection<HandoverItem> Items { get; set; } = new List<HandoverItem>();
    }

    public class HandoverItem
    {
        public Guid Id { get; set; }
        public Guid HandoverCaseId { get; set; }
        public Guid AssetAssignmentId { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING | DELIVERED
        public DateTimeOffset? DeliveredAt { get; set; }
        public Guid? DeliveredByAccountId { get; set; }
        // Beneficiary confirmed receipt (PIN + face scan).
        public DateTimeOffset? ClaimedAt { get; set; }
        public Guid? ClaimedByAccountId { get; set; }
        public HandoverCase? HandoverCase { get; set; }
        public AssetAssignment? AssetAssignment { get; set; }
    }
}
