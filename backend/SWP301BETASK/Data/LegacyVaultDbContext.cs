using Microsoft.EntityFrameworkCore;

namespace SWP301BETASK.Data;
using SWP301BETASK.Models;

public class LegacyVaultDbContext(DbContextOptions<LegacyVaultDbContext> options) : DbContext(options)
{
    // =====================================================
    // DBSETS
    // =====================================================  
    
    // Account / Identity
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();

    // Asset
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetSecret> AssetSecrets => Set<AssetSecret>();
    public DbSet<AssetVerification> AssetVerifications => Set<AssetVerification>();

    // Will
    public DbSet<Will> Wills => Set<Will>();
    public DbSet<WillAsset> WillAssets => Set<WillAsset>();
    public DbSet<BeneficiaryDesignation> BeneficiaryDesignations => Set<BeneficiaryDesignation>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();
    public DbSet<ExecutorAssignment> ExecutorAssignments => Set<ExecutorAssignment>();

    // DMS
    public DbSet<DmsConfiguration> DmsConfigurations => Set<DmsConfiguration>();
    public DbSet<DmsCheckIn> DmsCheckIns => Set<DmsCheckIn>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Username).HasMaxLength(50).IsRequired();
                e.Property(a => a.Email).HasMaxLength(254).IsRequired();
                e.Property(a => a.Phone).HasMaxLength(20);
                e.Property(a => a.PasswordHash).IsRequired();
                e.Property(a => a.SecretOtp).IsRequired();
                e.Property(a => a.Role).HasConversion<string>().HasMaxLength(32);
                e.Property(a => a.IdentityStatus).HasConversion<string>().HasMaxLength(16);

                e.HasIndex(a => a.Username).IsUnique();
                e.HasIndex(a => a.Email).IsUnique();   // supersedes Kiet's version
                e.HasIndex(a => a.Phone).IsUnique();
            });
            // =================================================
            // IDENTITY VERIFICATION
            // =================================================

            modelBuilder.Entity<IdentityVerification>(entity =>
            {
                entity.HasKey(i => i.Id);

                // Account 1 - N IdentityVerifications
                entity.HasOne(i => i.Account)
                      .WithMany()
                      .HasForeignKey(i => i.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            // =================================================
            // ASSET
            // =================================================

            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(a => a.Id);

                // Account 1 - N Assets
                entity.HasOne(a => a.OwnerAccount)
                      .WithMany()
                      .HasForeignKey(a => a.OwnerAccountId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Query Asset theo Owner thường xuyên
                entity.HasIndex(a => a.OwnerAccountId);
            });


            // =================================================
            // ASSET SECRET
            // =================================================

            modelBuilder.Entity<AssetSecret>(entity =>
            {
                // AssetId vừa là PK vừa là FK
                entity.HasKey(s => s.AssetId);

                // Asset 1 - 1 AssetSecret
                entity.HasOne(s => s.Asset)
                      .WithOne(a => a.Secret)
                      .HasForeignKey<AssetSecret>(s => s.AssetId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(s => s.KekVersion)
                      .HasDefaultValue(1);
            });


            // =================================================
            // ASSET VERIFICATION
            // =================================================

            modelBuilder.Entity<AssetVerification>(entity =>
            {
                entity.HasKey(v => v.Id);

                // Asset 1 - N AssetVerifications
                entity.HasOne(v => v.Asset)
                      .WithMany(a => a.Verifications)
                      .HasForeignKey(v => v.AssetId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(v => v.AssetId);
            });


            // =================================================
            // WILL
            // =================================================

            modelBuilder.Entity<Will>(entity =>
            {
                entity.HasKey(w => w.Id);

                // Account 1 - N Wills
                entity.HasOne(w => w.OwnerAccount)
                      .WithMany()
                      .HasForeignKey(w => w.OwnerAccountId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(w => w.OwnerAccountId);
            });


            // =================================================
            // WILL ASSET
            // =================================================

            modelBuilder.Entity<WillAsset>(entity =>
            {
                entity.HasKey(wa => wa.Id);

                // Will 1 - N WillAssets
                entity.HasOne(wa => wa.Will)
                      .WithMany()
                      .HasForeignKey(wa => wa.WillId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Asset 1 - N WillAssets
                entity.HasOne(wa => wa.Asset)
                      .WithMany()
                      .HasForeignKey(wa => wa.AssetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Không add cùng Asset 2 lần trong cùng Will
                entity.HasIndex(wa => new
                {
                    wa.WillId,
                    wa.AssetId
                })
                .IsUnique();
            });


            // =================================================
            // BENEFICIARY DESIGNATION
            // =================================================

            modelBuilder.Entity<BeneficiaryDesignation>(entity =>
            {
                entity.HasKey(b => b.Id);

                // Will 1 - N Beneficiaries
                entity.HasOne(b => b.Will)
                      .WithMany()
                      .HasForeignKey(b => b.WillId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Beneficiary có thể chưa có Account
                entity.HasOne(b => b.BeneficiaryAccount)
                      .WithMany()
                      .HasForeignKey(b => b.BeneficiaryAccountId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Trong cùng 1 Will không thêm trùng beneficiary email
                entity.HasIndex(b => new
                {
                    b.WillId,
                    b.Email
                })
                .IsUnique();
            });


            // =================================================
            // ASSET ASSIGNMENT
            // =================================================

            modelBuilder.Entity<AssetAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);

                // Assignment thuộc Asset trong Will
                entity.HasOne(a => a.WillAsset)
                      .WithMany()
                      .HasForeignKey(a => a.WillAssetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Assignment thuộc Beneficiary
                entity.HasOne(a => a.BeneficiaryDesignation)
                      .WithMany()
                      .HasForeignKey(a => a.BeneficiaryDesignationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Tránh assign cùng Asset cho cùng Beneficiary 2 lần
                entity.HasIndex(a => new
                {
                    a.WillAssetId,
                    a.BeneficiaryDesignationId
                })
                .IsUnique();
            });


            // =================================================
            // EXECUTOR ASSIGNMENT
            // =================================================

            modelBuilder.Entity<ExecutorAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Will 1 - N ExecutorAssignments
                entity.HasOne(e => e.Will)
                      .WithMany()
                      .HasForeignKey(e => e.WillId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Executor là một Account
                entity.HasOne(e => e.ExecutorAccount)
                      .WithMany()
                      .HasForeignKey(e => e.ExecutorAccountId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.WillId);

                entity.HasIndex(e => e.ExecutorAccountId);
            });


            // =================================================
            // DMS CONFIGURATION
            // =================================================

            modelBuilder.Entity<DmsConfiguration>(entity =>
            {
                entity.HasKey(d => d.Id);

                // 1 Will - 1 DmsConfiguration
                entity.HasOne(d => d.Will)
                      .WithOne()
                      .HasForeignKey<DmsConfiguration>(d => d.WillId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(d => d.WillId)
                      .IsUnique();
            });


            // =================================================
            // DMS CHECK-IN
            // =================================================

            modelBuilder.Entity<DmsCheckIn>(entity =>
            {
                entity.HasKey(c => c.Id);

                // DmsConfiguration 1 - N DmsCheckIns
                entity.HasOne(c => c.DmsConfiguration)
                      .WithMany()
                      .HasForeignKey(c => c.DmsConfigurationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.DmsConfigurationId);
            });
        }
    //Audit timestamps
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Account>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DateCreated = now;
                entry.Entity.DateUpdated = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.DateUpdated = now;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
