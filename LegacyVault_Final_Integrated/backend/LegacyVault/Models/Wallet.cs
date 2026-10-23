namespace LegacyVault.Models
{
    // LV-34/35: simulated wallet. No real money is involved.
    public class Wallet
    {
        public Guid AccountId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class WalletTransaction
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Type { get; set; } = string.Empty; // TOPUP | HOLD | RELEASE_IN | REFUND
        public decimal Amount { get; set; } // signed: negative reduces the balance
        public decimal BalanceAfter { get; set; }
        public Guid? WillId { get; set; }
        // Client-generated key prevents duplicate mock top-ups on retry/double-click.
        public string? IdempotencyKey { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    // Fee held from the Owner until the handover is verified complete.
    public class ServicePayment
    {
        public Guid Id { get; set; }
        public Guid WillId { get; set; }
        public Guid OwnerAccountId { get; set; }
        public Guid ExecutorAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = "HELD"; // HELD | RELEASED | REFUNDED
        public DateTimeOffset HeldAt { get; set; }
        public DateTimeOffset? ReleasedAt { get; set; }
        public DateTimeOffset? RefundedAt { get; set; }
        public Will? Will { get; set; }
    }
}
