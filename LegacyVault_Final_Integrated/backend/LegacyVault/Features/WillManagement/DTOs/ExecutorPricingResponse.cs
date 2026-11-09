namespace LegacyVault.Features.WillManagement.DTOs
{
    public class ExecutorPricingResponse
    {
        public decimal ServiceFee { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
