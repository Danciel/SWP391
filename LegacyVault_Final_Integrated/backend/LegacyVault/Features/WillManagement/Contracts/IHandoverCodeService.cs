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
    // UNAVAILABLE = delivered but no code could be issued (server setting / email problem).
    public sealed record AccessCodeStatus(
        string Status,
        bool Verified,
        DateTimeOffset? LockedUntil,
        DateTimeOffset? VerifiedUntil,
        int IssueCount = 0,
        int MaxIssues = 0,
        DateTimeOffset? NextResendAt = null,
        bool SupportRequested = false);

    // What the Executor / Owner sees per recipient (never the code itself).
    // Status: WAITING_REGISTRATION | NAME_MISMATCH | NOT_DELIVERED | CODE_PENDING |
    //         CODE_NOT_EMAILED | CODE_SENT | CODE_LOCKED | CODE_VERIFIED
    public sealed record RecipientCodeInfo(
        string Status,
        int IssueCount,
        DateTimeOffset? LockedUntil,
        DateTimeOffset? LastIssuedAt,
        bool SupportRequested);

    // Per-recipient handover access code (12 characters, XXXX-XXXX-XXXX).
    // Generated at delivery, emailed to the recipient, stored as a hash only.
    public interface IHandoverCodeService
    {
        // False when OwnerPin:Pepper is missing/short: no code can be created or checked.
        bool IsConfigured { get; }

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

        // Executor presses "Send a new code" for one recipient.
        Task<AccessCodeResult> ResendByExecutorAsync(
            Guid willId, Guid designationId, Guid executorAccountId,
            CancellationToken cancellationToken = default);

        // Recipient asks for help (new-code limit reached or code locked).
        Task<AccessCodeResult> RequestSupportAsync(
            Guid caseId, Guid accountId,
            CancellationToken cancellationToken = default);

        // Code state for every recipient of a case, keyed by designation id.
        Task<Dictionary<Guid, RecipientCodeInfo>> GetRecipientInfoAsync(
            Guid caseId, CancellationToken cancellationToken = default);
    }
}
