using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IHandoverService
    {
        // Owner presses "Immediate Handover" while alive (LV-32).
        Task<HandoverResponse> StartImmediateAsync(
            Guid willId, Guid ownerAccountId);

        // Executor, after DMS trigger + approved legal case + DECEASED (LV-33).
        Task<HandoverResponse> StartAfterDeathAsync(
            Guid willId, Guid executorAccountId);

        Task<HandoverResponse> GetAsync(Guid willId, Guid accountId);

        Task<HandoverResponse> DeliverAsync(
            Guid itemId, Guid executorAccountId);

        // Executor hands over EVERY pending item of the will's running handover
        // at once. Each recipient receives their own access code by email.
        Task<HandoverResponse> DeliverAllAsync(
            Guid willId, Guid executorAccountId);

        // Admin: handover could not be completed -> refund the held fee.
        Task<HandoverResponse> AbortAsync(Guid willId, Guid adminAccountId);

        // H06: stop deliveries while a dispute / problem is reviewed.
        // The held fee is untouched.
        Task<HandoverResponse> PauseAsync(
            Guid willId, Guid actorAccountId, string reason);

        // H06: continue a paused handover. Refused while a payment dispute
        // is OPEN, unless the dispute is being resolved with RESUME.
        Task<HandoverResponse> ResumeAsync(
            Guid willId, Guid adminAccountId, bool resolvingDispute = false);

        // H06 "revise": send one delivered item back to PENDING (paused only).
        Task<HandoverResponse> ReopenItemAsync(
            Guid itemId, Guid adminAccountId, string reason);

        // C9: the Executor cancels a death handover that delivered nothing yet.
        Task<HandoverResponse> CancelDeathHandoverAsync(
            Guid willId, Guid executorAccountId, string reason);

        // C9: the Executor's list of handovers with the next step.
        Task<List<ExecutorHandoverSummaryResponse>> GetExecutorHandoversAsync(
            Guid executorAccountId);

        // C2: minutes of the running / latest handover. Owner, Executor and
        // Admin see everything; a beneficiary sees only own lines.
        Task<HandoverMinutesResponse> GetMinutesAsync(
            Guid willId, Guid accountId, bool isAdmin);

        // C2: sealed PDF (Owner, Executor, Admin only).
        Task<(byte[] Pdf, string FileName, string Sha256)> GetMinutesPdfAsync(
            Guid willId, Guid accountId, bool isAdmin);

        // C3: the Executor signs the minutes (required before Deliver, C4).
        Task<HandoverMinutesResponse> SignAsExecutorAsync(
            Guid willId, Guid executorAccountId, string? ipAddress);

        // C3: a Beneficiary confirms receipt after claiming all own assets.
        Task<HandoverMinutesResponse> SignAsBeneficiaryAsync(
            Guid willId, Guid beneficiaryAccountId, string? ipAddress);

        // H06 "reassign": new executor for an ACTIVE or paused will.
        Task<ExecutorReassignmentResponse> ReassignExecutorAsync(
            Guid willId,
            Guid newExecutorAccountId,
            Guid adminAccountId,
            string reason);
    }
}
