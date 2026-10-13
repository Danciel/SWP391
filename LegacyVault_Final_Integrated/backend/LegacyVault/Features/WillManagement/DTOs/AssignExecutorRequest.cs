using System.ComponentModel.DataAnnotations;

namespace LegacyVault.Features.WillManagement.DTOs
{
    public class AssignExecutorRequest
    {
        [Required]
        public Guid ExecutorAccountId { get; set; }
    }
}
