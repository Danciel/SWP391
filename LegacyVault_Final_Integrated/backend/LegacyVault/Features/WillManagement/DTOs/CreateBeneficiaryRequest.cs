using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class CreateBeneficiaryRequest
    {
        // If the person was found by /api/accounts/search,
        // send this Id. Backend will copy trusted account identity fields.
        public Guid? BeneficiaryAccountId { get; set; }

        // Used only for a beneficiary who does not have a LegacyVault account.
        [MaxLength(120)]
        public string? FullName { get; set; }

        // Current V1 database schema requires beneficiary Email.
        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Relationship { get; set; }

        // Optional ID document number (only for unregistered people).
        [MaxLength(30)]
        public string? IdentityNumber { get; set; }
    }
}
