namespace LegacyVault.Models
{
    // Append-only operational audit trail. Never store secrets, credentials, or document contents here.
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? ActorAccountId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
