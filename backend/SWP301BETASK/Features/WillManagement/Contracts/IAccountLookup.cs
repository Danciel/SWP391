namespace SWP301BETASK.Features.WillManagement.Contracts
{
    public record AccountInfo(Guid Id, string FullName, string Email, string Phone);

    /// <summary>Read-only view of Accounts needed by WillManagement.</summary>
    public interface IAccountLookup
    {
        Task<AccountInfo?> GetAsync(Guid accountId, CancellationToken ct = default);
        Task<bool> IsIdentityVerifiedAsync(Guid accountId, CancellationToken ct = default);
    }
}
