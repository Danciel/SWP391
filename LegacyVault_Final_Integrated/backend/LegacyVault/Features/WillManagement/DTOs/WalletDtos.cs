using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class TopUpRequest
    {
        [Range(typeof(decimal), "1", "100000000")]
        public decimal Amount { get; set; }
    }

    public class WalletTransactionResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public Guid? WillId { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class WalletResponse
    {
        public decimal Balance { get; set; }
        public decimal HeldAmount { get; set; }
        public decimal PendingIncoming { get; set; }
        public decimal ExecutorDepositHeld { get; set; }
        public string Currency { get; set; } = "VND";
        public List<WalletTransactionResponse> Transactions { get; set; } = new();
    }
}
