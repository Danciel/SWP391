using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Contracts
{
    public interface IDmsService
    {
        Task<WillDto> SetAsync(Guid ownerId, Guid willId, SetDmsRequest req, CancellationToken ct);
        Task<WillDto> CheckInAsync(Guid ownerId, Guid willId, CancellationToken ct);
        Task<WillDto> RequestHandoverAsync(Guid ownerId, Guid willId, CancellationToken ct);
        Task<WillDto> CancelHandoverAsync(Guid ownerId, Guid willId, CancellationToken ct);

        /// <summary>Counts missed check-ins and triggers handover when MaxMissed is reached. Returns the number of wills triggered.</summary>
        Task<int> EvaluateOverdueAsync(DateTime utcNow, CancellationToken ct);
    }
}
