using LegacyVault.Data;
using LegacyVault.Features.AuditManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AuditManagement.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly LegacyVaultDbContext _db;

    public AuditRepository(LegacyVaultDbContext db)
    {
        _db = db;
    }

    public void Add(AuditLog log)
    {
        _db.AuditLogs.Add(log);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        string? action, string? entityType, Guid? actorAccountId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType.Trim());
        if (actorAccountId.HasValue) query = query.Where(x => x.ActorAccountId == actorAccountId.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}