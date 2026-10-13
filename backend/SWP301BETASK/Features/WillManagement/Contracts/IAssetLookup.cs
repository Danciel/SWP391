namespace SWP301BETASK.Features.WillManagement.Contracts
{
    /// <summary>What WillManagement needs from AssetManagement (Kiệt) - implemented by him.</summary>
    public interface IAssetLookup
    {
        /// <summary>Returns the subset of assetIds that exist and belong to ownerId.</summary>
        Task<HashSet<Guid>> GetOwnedAssetIdsAsync(Guid ownerId, IEnumerable<Guid> assetIds, CancellationToken ct = default);
    }
}
