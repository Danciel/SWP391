using Microsoft.EntityFrameworkCore;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                // Primary Key
                entity.HasKey(a => a.Id);

                // Email không được trùng
                entity.HasIndex(a => a.Email)
                      .IsUnique();
            });
        }
    }
}