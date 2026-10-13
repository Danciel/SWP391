using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Models;

namespace SWP301BETASK.Features.WillManagement.Contracts
{
    public interface IWillSubmissionService
    {
        Task<WillReviewDto> ReviewAsync(Guid ownerId, Guid willId, CancellationToken ct);
        Task<WillDto> SubmitAsync(Guid ownerId, Guid willId, CancellationToken ct);

        /// <summary>Throws 400 with the list of failed checks if the will cannot be submitted / re-hired.</summary>
        Task RequireSubmittableAsync(Will will, CancellationToken ct);

        /// <summary>Marks the executor assignment PENDING, holds the fee and starts the 7-day response window.</summary>
        void SendHireRequest(Will will);
    }
}
