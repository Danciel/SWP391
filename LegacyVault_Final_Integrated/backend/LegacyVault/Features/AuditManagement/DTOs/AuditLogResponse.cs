namespace LegacyVault.Features.AuditManagement.DTOs;

public class AuditLogResponse
{
    public Guid Id { get; set; }

    // Actor Information
    public Guid? ActorAccountId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; } // <-- NEW

    public string Action { get; set; } = string.Empty;

    // Entity Information
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }

    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}