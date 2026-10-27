using LegacyVault.Models;

namespace LegacyVault.Features.AuditManagement.Contracts;

public interface IAuditRepository
{
    void Add(AuditLog log);
    Task SaveChangesAsync();
    Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        string? action,
        string? entityType,
        Guid? actorAccountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize);
}