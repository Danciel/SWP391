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

        // H06 "reassign": new executor for an ACTIVE or paused will.
        Task<ExecutorReassignmentResponse> ReassignExecutorAsync(
            Guid willId,
            Guid newExecutorAccountId,
            Guid adminAccountId,
            string reason);
    }
}
