using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class LinkBeneficiaryRequest
    {
        [Required]
        public Guid AccountId { get; set; }
    }
}
