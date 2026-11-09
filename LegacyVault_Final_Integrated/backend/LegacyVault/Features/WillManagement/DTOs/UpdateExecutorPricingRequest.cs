using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class UpdateExecutorPricingRequest
    {
        [Range(typeof(decimal), "0", "1000000000000")]
        public decimal ServiceFee { get; set; }

        [Required]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter uppercase currency code.")]
        public string Currency { get; set; } = "VND";
    }
}
