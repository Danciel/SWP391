using LegacyVault.Models;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface IDmsRuleRepository
    {
        Task<DmsRule?> GetAsync(CancellationToken ct = default);
        Task AddAsync(DmsRule rule, CancellationToken ct = default);
        void Update(DmsRule rule);
        void Remove(DmsRule rule);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}