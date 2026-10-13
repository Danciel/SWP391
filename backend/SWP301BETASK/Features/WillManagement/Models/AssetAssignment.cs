namespace SWP301BETASK.Features.WillManagement.Models
{
    /// <summary>An asset selected into a will, optionally assigned to one beneficiary.</summary>
    public class AssetAssignment
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Will Will { get; set; } = null!;
        /// <summary>Asset.Id (AssetManagement). No FK on purpose.</summary>
        public Guid AssetId { get; set; }
        public Guid? BeneficiaryDesignationId { get; set; }
        public BeneficiaryDesignation? Beneficiary { get; set; }
        public Guid AssignedByOwnerId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string AssignmentStatus { get; set; } = AssignmentStatuses.Unassigned;
        public string HandoverStatus { get; set; } = HandoverStatuses.Pending;
    }
}
