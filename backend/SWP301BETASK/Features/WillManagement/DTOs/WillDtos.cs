namespace SWP301BETASK.Features.WillManagement.DTOs
{
    // Response shapes mirror what the FE (mockVault.js) already reads.
    public record BeneficiaryDto(Guid Id, Guid? AccountId, string FullName, string Email, string Phone, string? IdentityNumber, string Relationship);

    public record WillAssetDto(Guid AssetId, string AssignmentStatus, string HandoverStatus, BeneficiaryDto? Beneficiary);

    public record ExecutorDto(Guid Id, string Name, string Specialization, string License, double Rating, int Reviews,
        int ExperienceYears, int CompletedCases, decimal Fee, bool Available);

    public record DmsDto(bool Enabled, int CheckInDays, int ReminderDays, int MaxMissed, string Status, int MissedCount,
        DateTime? LastCheckIn, DateTime? NextDueAt);

    public record PaymentDto(string Status, decimal Fee);

    public record HireRequestDto(Guid RequestId, string Status, DateTime? RequestedAt, DateTime? DeadlineAt);

    public record WillDto(
        Guid Id, string Name, string Description, string Status,
        List<WillAssetDto> Assets,
        List<BeneficiaryDto> Beneficiaries,
        ExecutorDto? Executor, DmsDto Dms, PaymentDto Payment, HireRequestDto? HireRequest,
        DateTime CreatedAt, DateTime UpdatedAt);
}

namespace SWP301BETASK.Features.WillManagement.DTOs
{
    public record ReviewCheckDto(string Key, bool Passed, string Message);

    /// <summary>GET /api/wills/{id}/review. canSubmit is true only for a DRAFT with every check passed.</summary>
    public record WillReviewDto(WillDto Will, bool IdentityVerified, List<ReviewCheckDto> Checks, bool CanSubmit, List<string> Errors);
}
