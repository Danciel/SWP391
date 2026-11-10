namespace LegacyVault.Features.WillManagement.Contracts
{
    // N7 (Nhien): lock / unlock the OWNER account when a death certificate
    // (LegalCase) is APPROVED / cancelled. Until Nhien's real implementation
    // exists, MockAccountLockService only writes an audit log.
    //
    // The real implementation must also make JWTs issued BEFORE the lock stop
    // working (for example a LockedAt / TokenVersion check in the JWT events).
    public interface IAccountLockService
    {
        Task LockOwnerAsync(
            Guid ownerAccountId,
            Guid legalCaseId,
            string reason,
            CancellationToken ct = default);

        Task UnlockOwnerAsync(
            Guid ownerAccountId,
            string reason,
            CancellationToken ct = default);
    }
}
