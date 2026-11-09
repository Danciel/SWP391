using LegacyVault.Features.WillManagement.Contracts;

namespace LegacyVault.Features.WillManagement.Services
{
    public class NullLifeStatusClient : ILifeStatusClient
    {
        public Task<string> GetStatusAsync(
            Guid ownerAccountId,
            CancellationToken ct)
            => Task.FromResult("UNKNOWN");
    }
}
