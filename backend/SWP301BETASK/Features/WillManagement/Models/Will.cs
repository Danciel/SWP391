namespace SWP301BETASK.Features.WillManagement.Models
{
    public class Will
    {
        public Guid Id { get; set; }
        /// <summary>Account.Id of the owner. No FK on purpose (Accounts belongs to IdentityAccess).</summary>
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = WillStatus.Draft;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<BeneficiaryDesignation> Beneficiaries { get; set; } = new();
        public List<AssetAssignment> Assignments { get; set; } = new();
        public ExecutorAssignment? Executor { get; set; }
        public DmsConfiguration? Dms { get; set; }
    }
}
