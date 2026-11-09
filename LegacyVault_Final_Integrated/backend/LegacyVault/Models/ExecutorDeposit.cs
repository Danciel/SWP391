namespace LegacyVault.Models;

// Per-Will executor security deposit (20% of the agreed service fee).
public class ExecutorDeposit
{
    public Guid Id { get; set; }
    public Guid WillId { get; set; }
    public Guid ExecutorAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "HELD";
    public DateTimeOffset HeldAt { get; set; }
}
