using System.ComponentModel.DataAnnotations;

namespace SWP301BETASK.Features.WillManagement.DTOs
{
    public class CreateWillRequest
    {
        [MaxLength(200)] public string? Name { get; set; }
        [MaxLength(2000)] public string? Description { get; set; }
    }

    public class UpdateWillRequest
    {
        [MaxLength(200)] public string? Name { get; set; }
        [MaxLength(2000)] public string? Description { get; set; }
    }

    public class SetWillAssetsRequest
    {
        [Required] public List<Guid> AssetIds { get; set; } = new();
    }

    public class BeneficiaryRequest
    {
        /// <summary>Optional: link to an existing account. Overrides fullName/email/phone/identityNumber.</summary>
        public Guid? AccountId { get; set; }
        [MaxLength(200)] public string? FullName { get; set; }
        [EmailAddress, MaxLength(256)] public string? Email { get; set; }
        [MaxLength(30)] public string? Phone { get; set; }
        [MaxLength(20)] public string? IdentityNumber { get; set; }
        [MaxLength(50)] public string? Relationship { get; set; }
    }

    public class AssignAssetRequest
    {
        [Required] public Guid AssetId { get; set; }
        /// <summary>Null = remove the beneficiary from this asset.</summary>
        public Guid? BeneficiaryId { get; set; }
    }

    // ---- used in part 2 ----
    public class SetExecutorRequest
    {
        [Required] public Guid ExecutorId { get; set; }
    }

    public class SetDmsRequest
    {
        public int CheckInDays { get; set; } = 30;
        public int ReminderDays { get; set; } = 3;
        public int MaxMissed { get; set; } = 2;
    }
}
