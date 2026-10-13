using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Contracts
{
    public interface IWillService
    {
        Task<WillDto> CreateAsync(Guid ownerId, CreateWillRequest req, CancellationToken ct);
        Task<List<WillDto>> ListAsync(Guid ownerId, CancellationToken ct);
        Task<WillDto> GetAsync(Guid ownerId, Guid willId, CancellationToken ct);
        Task<WillDto> UpdateAsync(Guid ownerId, Guid willId, UpdateWillRequest req, CancellationToken ct);
        Task DeleteDraftAsync(Guid ownerId, Guid willId, CancellationToken ct);
        Task<WillDto> SetAssetsAsync(Guid ownerId, Guid willId, SetWillAssetsRequest req, CancellationToken ct);
    }
}
