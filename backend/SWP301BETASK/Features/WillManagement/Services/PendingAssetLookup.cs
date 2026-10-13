using SWP301BETASK.Features.WillManagement.Contracts;

namespace SWP301BETASK.Features.WillManagement.Services
{
    /// <summary>
    /// TEMPORARY stand-in until AssetManagement (Kiệt) has an Assets table + real IAssetLookup.
    /// Development: accepts every asset id so the Will flow can be tested. Other environments: accepts nothing (fail closed).
    /// Kiệt: register your implementation BEFORE AddWillManagement() (it uses TryAdd) and delete this file.
    /// </summary>
    internal sealed class PendingAssetLookup : IAssetLookup
    {
        private readonly IHostEnvironment _env;
        public PendingAssetLookup(IHostEnvironment env) => _env = env;

        public Task<HashSet<Guid>> GetOwnedAssetIdsAsync(Guid ownerId, IEnumerable<Guid> assetIds, CancellationToken ct = default) =>
            Task.FromResult(_env.IsDevelopment() ? assetIds.ToHashSet() : new HashSet<Guid>());
    }
}
