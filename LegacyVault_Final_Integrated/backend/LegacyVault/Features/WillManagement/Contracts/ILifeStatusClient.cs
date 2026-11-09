namespace LegacyVault.Features.WillManagement.Contracts
{
    // D05: Nhien's LifeStatus API (R10: ALIVE / UNKNOWN / PENDING / DECEASED).
    // Until it exists, NullLifeStatusClient answers UNKNOWN.
    public interface ILifeStatusClient
    {
        Task<string> GetStatusAsync(
            Guid ownerAccountId,
            CancellationToken ct);
    }
}
