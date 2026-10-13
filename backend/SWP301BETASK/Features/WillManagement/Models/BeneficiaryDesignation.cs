namespace SWP301BETASK.Features.WillManagement.Models
{
    public class BeneficiaryDesignation
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Will Will { get; set; } = null!;
        /// <summary>Null when the beneficiary has no account yet ("New Beneficiary" tab in FE).</summary>
        public Guid? BeneficiaryAccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? IdentityNumber { get; set; }
        public string Relationship { get; set; } = "Other";
        public DateTime CreatedAt { get; set; }
    }
}
