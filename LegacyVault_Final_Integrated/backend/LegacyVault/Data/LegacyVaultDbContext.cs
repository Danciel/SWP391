using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Data
{
    public class LegacyVaultDbContext : DbContext
    {
        public LegacyVaultDbContext(
            DbContextOptions<LegacyVaultDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<EmailOtp> EmailOtps { get; set; }

        public DbSet<ExecutorProfile> ExecutorProfiles { get; set; }

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetSecret> AssetSecrets { get; set; }

        public DbSet<AssetVerification> AssetVerifications { get; set; }

        public DbSet<MockExternalAccount> MockExternalAccounts { get; set; }

        public DbSet<Will> Wills { get; set; }

        public DbSet<WillAsset> WillAssets { get; set; }

        public DbSet<BeneficiaryDesignation> BeneficiaryDesignations { get; set; }

        public DbSet<AssetAssignment> AssetAssignments { get; set; }

        public DbSet<ExecutorAssignment> ExecutorAssignments { get; set; }

        public DbSet<DmsConfiguration> DmsConfigurations { get; set; }

        public DbSet<DmsCheckIn> DmsCheckIns { get; set; }

        public DbSet<Face> Faces { get; set; }

        public DbSet<IdpUser> IdpUsers { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // ACCOUNT
            // Existing Auth mapping kept intact.
            // =========================
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("Accounts");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.FullName)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.Phone)
                    .HasMaxLength(30);

                entity.Property(x => x.PasswordHash)
                    .IsRequired();

                entity.Property(x => x.Role)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.IdentityStatus)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.ExternalIdentityId)
                    .HasMaxLength(255);
            });

            // =========================
            // EMAIL OTP
            // Existing Auth mapping kept intact.
            // =========================
            modelBuilder.Entity<EmailOtp>(entity =>
            {
                entity.ToTable("EmailOtps");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.OtpHash)
                    .IsRequired();

                entity.Property(x => x.ExpiresAt)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // EXECUTOR PROFILE
            // =========================
            modelBuilder.Entity<ExecutorProfile>(entity =>
            {
                entity.ToTable("ExecutorProfiles");

                entity.HasKey(x => x.AccountId);

                entity.Property(x => x.Rating)
                    .HasPrecision(3, 2);

                entity.HasOne(x => x.Account)
                    .WithOne()
                    .HasForeignKey<ExecutorProfile>(
                        x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // ASSET
            // =========================
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.ToTable("Assets");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.AssetType)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.Property(x => x.VerificationStatus)
                    .IsRequired();

                entity.HasOne(x => x.OwnerAccount)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Secret)
                    .WithOne(x => x.Asset)
                    .HasForeignKey<AssetSecret>(
                        x => x.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssetSecret>(entity =>
            {
                entity.ToTable("AssetSecrets");

                entity.HasKey(x => x.AssetId);

                entity.Property(x => x.EncryptedPayload)
                    .IsRequired();

                entity.Property(x => x.DataNonce)
                    .IsRequired();

                entity.Property(x => x.DataTag)
                    .IsRequired();

                entity.Property(x => x.EncryptedDek)
                    .IsRequired();

                entity.Property(x => x.DekNonce)
                    .IsRequired();

                entity.Property(x => x.DekTag)
                    .IsRequired();

                entity.Property(x => x.EncryptionAlgorithm)
                    .IsRequired();
            });

            modelBuilder.Entity<AssetVerification>(entity =>
            {
                entity.ToTable("AssetVerifications");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasOne(x => x.Asset)
                    .WithMany(x => x.Verifications)
                    .HasForeignKey(x => x.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // PERMANENT DEMO MOCK PROVIDER DB
            // =========================
            modelBuilder.Entity<MockExternalAccount>(entity =>
            {
                entity.ToTable("MockExternalAccounts");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Provider)
                    .IsRequired();

                entity.Property(x => x.Username)
                    .IsRequired();

                entity.Property(x => x.Password)
                    .IsRequired();

                entity.Property(x => x.DisplayName)
                    .IsRequired();

                entity.HasIndex(x =>
                    new
                    {
                        x.Provider,
                        x.Username
                    })
                    .IsUnique();
            });

            // =========================
            // WILL
            // =========================
            modelBuilder.Entity<Will>(entity =>
            {
                entity.ToTable("Wills");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Title)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasOne(x => x.OwnerAccount)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ExecutorAssignment)
                    .WithOne(x => x.Will)
                    .HasForeignKey<ExecutorAssignment>(
                        x => x.WillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.DmsConfiguration)
                    .WithOne(x => x.Will)
                    .HasForeignKey<DmsConfiguration>(
                        x => x.WillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WillAsset>(entity =>
            {
                entity.ToTable("WillAssets");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x =>
                    new
                    {
                        x.WillId,
                        x.AssetId
                    })
                    .IsUnique();

                entity.HasOne(x => x.Will)
                    .WithMany(x => x.WillAssets)
                    .HasForeignKey(x => x.WillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Asset)
                    .WithMany()
                    .HasForeignKey(x => x.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BeneficiaryDesignation>(entity =>
            {
                entity.ToTable("BeneficiaryDesignations");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.FullName)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasIndex(x =>
                    new
                    {
                        x.WillId,
                        x.Email
                    })
                    .IsUnique();

                entity.HasOne(x => x.Will)
                    .WithMany(x => x.Beneficiaries)
                    .HasForeignKey(x => x.WillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.BeneficiaryAccount)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.BeneficiaryAccountId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AssetAssignment>(entity =>
            {
                entity.ToTable("AssetAssignments");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x =>
                    new
                    {
                        x.WillAssetId,
                        x.BeneficiaryDesignationId
                    })
                    .IsUnique();

                entity.HasOne(x => x.WillAsset)
                    .WithMany(x => x.Assignments)
                    .HasForeignKey(x => x.WillAssetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x =>
                    x.BeneficiaryDesignation)
                    .WithMany(x =>
                        x.AssetAssignments)
                    .HasForeignKey(x =>
                        x.BeneficiaryDesignationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExecutorAssignment>(entity =>
            {
                entity.ToTable("ExecutorAssignments");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasIndex(x => x.WillId);

                entity.HasOne(x => x.ExecutorAccount)
                    .WithMany()
                    .HasForeignKey(x =>
                        x.ExecutorAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DmsConfiguration>(entity =>
            {
                entity.ToTable("DmsConfigurations");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.WillId)
                    .IsUnique();

                entity.Property(x => x.Status)
                    .IsRequired();
            });

            modelBuilder.Entity<DmsCheckIn>(entity =>
            {
                entity.ToTable("DmsCheckIns");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.HasOne(x => x.DmsConfiguration)
                    .WithMany(x => x.CheckIns)
                    .HasForeignKey(x =>
                        x.DmsConfigurationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // IDP USER
            // =========================
            modelBuilder.Entity<IdpUser>(entity =>
            {
                entity.ToTable("IdpUsers");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.FullName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(x => x.Phone)
                    .HasMaxLength(32);

                entity.Property(x => x.IdentityStatus)
                    .HasMaxLength(32)
                    .IsRequired();

                entity.HasIndex(x => x.Email).IsUnique();

                // Denormalized pointer, managed by the face service.
                // No navigation property → EF will NOT create a second relationship.
                entity.Property(x => x.FaceId);
            });

            // =========================
            // FACE (one-to-one, owned by IdpUser)
            // =========================
            modelBuilder.Entity<Face>(entity =>
            {
                entity.ToTable("Faces");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.EmbeddingJson).IsRequired();

                entity.Property(x => x.CreatedAt).IsRequired();

                entity.Property(x => x.UpdatedAt).IsRequired();

                entity.HasIndex(x => x.IdpUserId).IsUnique();

                entity.HasOne(x => x.IdpUser)
                    .WithOne()
                    .HasForeignKey<Face>(x => x.IdpUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
