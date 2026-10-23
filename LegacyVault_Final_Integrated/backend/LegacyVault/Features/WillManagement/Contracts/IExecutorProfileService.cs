using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IExecutorProfileService
    {
        Task<ExecutorProfileResponse> UpdateMyProfileAsync(
            UpdateExecutorProfileRequest request);

        Task<ExecutorProfileResponse> GetProfileAsync(Guid accountId);

        // E10: reference prices built from platform data only.
        Task<ExecutorPricingReferenceResponse> GetPricingReferenceAsync();

        Task<List<ExecutorReviewResponse>> GetReviewsAsync(Guid accountId);

        Task<ExecutorReviewResponse> CreateReviewAsync(
            Guid executorAccountId,
            CreateExecutorReviewRequest request);
    }
}
