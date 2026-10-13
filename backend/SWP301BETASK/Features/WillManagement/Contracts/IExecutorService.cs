using SWP301BETASK.Features.WillManagement.DTOs;

namespace SWP301BETASK.Features.WillManagement.Contracts
{
    public interface IExecutorService
    {
        Task<List<ExecutorDto>> ListAsync(CancellationToken ct);
        Task<WillDto> SetAsync(Guid ownerId, Guid willId, SetExecutorRequest req, CancellationToken ct);

        /// <summary>
        /// Hook for whoever builds the Executor "accept/decline hire request" flow (outside this task).
        /// accepted -> ACTIVE, payment PAID, DMS ACTIVE; declined -> NEEDS_EXECUTOR, payment RELEASED.
        /// </summary>
        Task ResolveHiringAsync(Guid willId, bool accepted, CancellationToken ct);
    }
}
