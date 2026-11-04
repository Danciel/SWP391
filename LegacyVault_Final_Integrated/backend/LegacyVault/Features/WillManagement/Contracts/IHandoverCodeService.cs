namespace LegacyVault.Features.WillManagement.Contracts
{
    // Result of a code action. Message is safe to show to the user.
    public sealed record AccessCodeResult(
        bool Success,
        int StatusCode,
        string Message,
        int? RemainingAttempts = null,
        DateTimeOffset? LockedUntil = null);

    // Status: NOT_ISSUED | SENT | VERIFIED | LOCKED
    public sealed record AccessCodeStatus(
        string Status,
        bool Verified,
        DateTimeOffset? LockedUntil,
        DateTimeOffset? VerifiedUntil);

    // Per-recipient handover access code (12 characters, XXXX-XXXX-XXXX).
    // Generated at delivery, emailed to the recipient, stored as a hash only.
    public interface IHandoverCodeService
    {
        // Issues + emails a code to every recipient that has delivered items,
        // a linked account and no code yet. Never throws.
        Task IssueForCaseAsync(Guid caseId, CancellationToken cancellationToken = default);

        // Same, for the running handover of a will.
        Task IssueForWillAsync(Guid willId, CancellationToken cancellationToken = default);

        // The recipient types the code (3 wrong tries lock it for 15 minutes).
        Task<AccessCodeResult> VerifyAsync(
            Guid caseId, Guid accountId, string? code,
            CancellationToken cancellationToken = default);

        // "Send again": a NEW code replaces the old one.
        Task<AccessCodeResult> ResendAsync(
            Guid caseId, Guid accountId,
            CancellationToken cancellationToken = default);

        Task<AccessCodeStatus> GetStatusAsync(
            Guid caseId, Guid designationId,
            CancellationToken cancellationToken = default);
    }
}
