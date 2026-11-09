using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IExecutorPricingService
    {
        Task<ExecutorPricingRule> GetRulesAsync();

        Task<ExecutorPricingRuleResponse> GetRulesResponseAsync();

        Task<ExecutorPricingRuleResponse> UpdateRulesAsync(
            Guid adminAccountId, UpdateExecutorPricingRuleRequest request);

        Task EnrichListAsync(List<ExecutorListItemResponse> items);

        Task EnrichProfileAsync(ExecutorProfileResponse profile);

        Task<ExecutorPricingSuggestionResponse> GetMineAsync(Guid executorAccountId);
    }
}
