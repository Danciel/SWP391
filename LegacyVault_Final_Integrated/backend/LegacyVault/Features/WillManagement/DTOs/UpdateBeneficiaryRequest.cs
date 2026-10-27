using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
	public class UpdateBeneficiaryRequest
	{
		[Required]
		[MaxLength(120)]
		public string FullName { get; set; } = string.Empty;

		[EmailAddress]
		[MaxLength(255)]
		public string? Email { get; set; }

		[MaxLength(30)]
		public string? Phone { get; set; }

		[MaxLength(100)]
		public string? Relationship { get; set; }
	}
}