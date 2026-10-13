using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Contracts
{
    public interface IBeneficiaryService
    {
        Task<WillDto> AddAsync(Guid ownerId, Guid willId, BeneficiaryRequest req, CancellationToken ct);
        Task<WillDto> UpdateAsync(Guid ownerId, Guid willId, Guid beneficiaryId, BeneficiaryRequest req, CancellationToken ct);
        Task<WillDto> RemoveAsync(Guid ownerId, Guid willId, Guid beneficiaryId, CancellationToken ct);
        Task<WillDto> AssignAsync(Guid ownerId, Guid willId, AssignAssetRequest req, CancellationToken ct);
    }
}
