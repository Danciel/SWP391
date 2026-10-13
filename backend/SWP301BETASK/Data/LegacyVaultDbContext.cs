using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Features.WillManagement;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Models;
namespace SWP301BETASK.Data
{
    public class LegacyVaultDbContext : DbContext
    {
        public LegacyVaultDbContext(
           DbContextOptions<LegacyVaultDbContext> options)
           : base(options)
        {

        }
        public DbSet<Account> Accounts { get; set; }

        // ---- WillManagement (Cường) ----
        public DbSet<Will> Wills { get; set; }
        public DbSet<BeneficiaryDesignation> BeneficiaryDesignations { get; set; }
        public DbSet<AssetAssignment> AssetAssignments { get; set; }
        public DbSet<ExecutorProfile> ExecutorProfiles { get; set; }
        public DbSet<ExecutorAssignment> ExecutorAssignments { get; set; }
        public DbSet<DmsConfiguration> DmsConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureWillManagement();
        }
    }
}
