using LegacyVault.Features.AuditManagement.DTOs;

namespace LegacyVault.Features.AuditManagement.Contracts;

public interface IAuditService
{
    Task RecordAsync(
        Guid? actorId,
        string action,
        string entityType,
        Guid? entityId,
        object? details = null,
        bool saveImmediately = false);

    Task<(List<AuditLogResponse> Items, int Total)> GetLogsAsync(
        string? action,
        string? entityType,
        Guid? actorAccountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize);
}