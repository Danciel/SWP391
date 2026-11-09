namespace LegacyVault.Models
{
    public class MockExternalAccount
    {
        public Guid Id { get; set; }

        public string Provider { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        // DEMO ONLY: fake plaintext password by current project decision.
        // Never put real external-provider credentials in this table.
        public string Password { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
