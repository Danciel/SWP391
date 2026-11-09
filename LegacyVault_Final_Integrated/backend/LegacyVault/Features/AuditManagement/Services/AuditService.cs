using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Features.AuditManagement.Contracts;
using LegacyVault.Features.AuditManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.AuditManagement.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;
    private readonly LegacyVaultDbContext _db;

    public AuditService(IAuditRepository repository, LegacyVaultDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task RecordAsync(
        Guid? actorId, string action, string entityType, Guid? entityId,
        object? details = null, bool saveImmediately = false)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorAccountId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details == null ? null : JsonSerializer.Serialize(details),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repository.Add(log);

        if (saveImmediately)
        {
            await _repository.SaveChangesAsync();
        }
    }

    public async Task<(List<AuditLogResponse> Items, int Total)> GetLogsAsync(
        string? action, string? entityType, Guid? actorAccountId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _repository.GetPagedAsync(
            action, entityType, actorAccountId, from, to, page, pageSize);

        // 1. Resolve Actor Names AND Roles
        var actorIds = items.Where(x => x.ActorAccountId.HasValue)
                            .Select(x => x.ActorAccountId!.Value)
                            .Distinct()
                            .ToList();

        var actors = await _db.Accounts
            .Where(a => actorIds.Contains(a.Id))
            .Select(a => new { a.Id, a.FullName, a.Role })
            .ToDictionaryAsync(a => a.Id, a => a);

        // 2. Resolve Entity Names
        var entityNames = await ResolveEntityNamesAsync(items);

        var responses = items.Select(x =>
        {
            // Safely extract actor details if they exist
            string? actorName = "System";
            string? actorRole = "SYSTEM";

            if (x.ActorAccountId.HasValue && actors.TryGetValue(x.ActorAccountId.Value, out var actor))
            {
                actorName = actor.FullName;
                actorRole = actor.Role;
            }

            return new AuditLogResponse
            {
                Id = x.Id,
                ActorAccountId = x.ActorAccountId,
                ActorName = actorName,
                ActorRole = actorRole, // <-- Populated here
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                EntityName = x.EntityId.HasValue && entityNames.TryGetValue(x.EntityId.Value, out var eName)
                    ? eName
                    : (x.EntityId.HasValue ? x.EntityId.Value.ToString()[..8] + "..." : "N/A"),
                Details = x.Details,
                CreatedAt = x.CreatedAt
            };
        }).ToList();

        return (responses, total);
    }

    // Helper method to resolve polymorphic entity names
    private async Task<Dictionary<Guid, string>> ResolveEntityNamesAsync(List<AuditLog> logs)
    {
        var result = new Dictionary<Guid, string>();

        var willIds = logs.Where(x => x.EntityType == "Will" && x.EntityId.HasValue)
                          .Select(x => x.EntityId!.Value).Distinct().ToList();

        var accountIds = logs.Where(x => x.EntityType == "Account" && x.EntityId.HasValue)
                             .Select(x => x.EntityId!.Value).Distinct().ToList();

        var paymentIds = logs.Where(x => x.EntityType == "ServicePayment" && x.EntityId.HasValue)
                             .Select(x => x.EntityId!.Value).Distinct().ToList();

        if (willIds.Any())
        {
            var wills = await _db.Wills.Where(w => willIds.Contains(w.Id))
                                       .ToDictionaryAsync(w => w.Id, w => w.Title);
            foreach (var kvp in wills) result[kvp.Key] = kvp.Value;
        }

        if (accountIds.Any())
        {
            var accounts = await _db.Accounts.Where(a => accountIds.Contains(a.Id))
                                             .ToDictionaryAsync(a => a.Id, a => a.FullName);
            foreach (var kvp in accounts) result[kvp.Key] = kvp.Value;
        }

        if (paymentIds.Any())
        {
            var payments = await _db.ServicePayments.Where(p => paymentIds.Contains(p.Id))
                                                    .ToDictionaryAsync(p => p.Id, p => $"{p.Amount:N0} {p.Currency}");
            foreach (var kvp in payments) result[kvp.Key] = kvp.Value;
        }

        return result;
    }
}