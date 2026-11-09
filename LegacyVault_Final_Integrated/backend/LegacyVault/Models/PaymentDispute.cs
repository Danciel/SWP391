namespace LegacyVault.Models
{
    // Mock-wallet dispute tied to one Will's fee. Money is settled only through WalletService.
    public class PaymentDispute
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Guid RaisedByAccountId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "OPEN"; // OPEN | RESOLVED
        public string? ResolutionAction { get; set; } // REFUND | RELEASE
        public string? ResolutionNote { get; set; }
        public Guid? ResolvedByAccountId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
    }
}
