namespace SWP301BETASK.Features.WillManagement.Models
{
    /// <summary>Marketplace profile shown in GET /api/executors (fee, rating, licence...).</summary>
    public class ExecutorProfile
    {
        public Guid Id { get; set; }
        /// <summary>Account.Id of the executor user, once executors can log in.</summary>
        public Guid? AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public int ExperienceYears { get; set; }
        public int CompletedCases { get; set; }
        public decimal Fee { get; set; }
        public bool Available { get; set; } = true;
    }
}
