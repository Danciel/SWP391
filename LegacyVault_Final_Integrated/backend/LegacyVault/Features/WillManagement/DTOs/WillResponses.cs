namespace LegacyVault.Features.WillManagement.DTOs
{
    public class WillSummaryResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class WillAssetResponse
    {
        public Guid WillAssetId { get; set; }

        public Guid AssetId { get; set; }

        public string AssetName { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;
    }

    public class BeneficiaryResponse
    {
        public Guid Id { get; set; }

        public Guid? BeneficiaryAccountId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Relationship { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class AssetAssignmentResponse
    {
        public Guid Id { get; set; }

        public Guid WillAssetId { get; set; }

        public Guid BeneficiaryDesignationId { get; set; }

        public string? BeneficiaryName { get; set; }

        public string? Relationship { get; set; }
    }

    public class ExecutorAssignmentResponse
    {
        public Guid Id { get; set; }

        public Guid ExecutorAccountId { get; set; }

        public string ExecutorName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    public class DmsConfigurationResponse
    {
        public Guid Id { get; set; }

        public bool Enabled { get; set; }

        public int CheckInIntervalDays { get; set; }

        public int ReminderBeforeDays { get; set; }

        public int MaxMissedCheckIns { get; set; }

        public int MissedCheckIns { get; set; }

        public DateTimeOffset? LastCheckInAt { get; set; }

        public DateTimeOffset? NextCheckInAt { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class ExecutorListItemResponse
    {
        public Guid AccountId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public decimal Rating { get; set; }

        public int CompletedCases { get; set; }

        public int ExperienceYears { get; set; }

        public string? Bio { get; set; }
    }

    public class WillDetailResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ActivatedAt { get; set; }

        public List<WillAssetResponse> Assets { get; set; } = new();

        public List<BeneficiaryResponse> Beneficiaries { get; set; } = new();

        public List<AssetAssignmentResponse> Assignments { get; set; } = new();

        public ExecutorAssignmentResponse? Executor { get; set; }

        public DmsConfigurationResponse? Dms { get; set; }
    }
}
