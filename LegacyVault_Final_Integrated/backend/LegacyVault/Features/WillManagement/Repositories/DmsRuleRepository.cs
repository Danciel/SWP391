using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Repositories
{
    public class DmsRuleRepository : IDmsRuleRepository
    {
        private readonly LegacyVaultDbContext _db;

        public DmsRuleRepository(LegacyVaultDbContext db)
        {
            _db = db;
        }

        public Task<DmsRule?> GetAsync(CancellationToken ct = default) =>
            _db.DmsRules.FirstOrDefaultAsync(
                x => x.Id == DmsRule.SingletonId, ct);

        public async Task AddAsync(
            DmsRule rule, CancellationToken ct = default) =>
            await _db.DmsRules.AddAsync(rule, ct);

        public void Update(DmsRule rule) =>
            _db.DmsRules.Update(rule);

        public void Remove(DmsRule rule) =>
            _db.DmsRules.Remove(rule);

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);
    }
}