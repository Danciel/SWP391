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

        public DbSet<ExecutorReview> ExecutorReviews { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<PaymentDispute> PaymentDisputes { get; set; }

        public DbSet<LegalCase> LegalCases { get; set; }

        public DbSet<HandoverCase> HandoverCases { get; set; }

        public DbSet<HandoverItem> HandoverItems { get; set; }

        public DbSet<Wallet> Wallets { get; set; }

        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        public DbSet<ServicePayment> ServicePayments { get; set; }

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

                entity.Property(x => x.ServiceFee)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0m);

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("VND")
                    .IsRequired();

                entity.Property(x => x.SuspensionReason).HasMaxLength(500);

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

                entity.Property(x => x.AllocationPercentage)
                    .HasPrecision(5, 2)
                    .HasDefaultValue(100m);

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

                entity.Property(x => x.ServiceFeeSnapshot)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0m);

                entity.Property(x => x.CurrencySnapshot)
                    .HasMaxLength(3)
                    .HasDefaultValue("VND")
                    .IsRequired();

                entity.Property(x => x.Scope)
                    .HasMaxLength(16)
                    .HasDefaultValue("WILL")
                    .IsRequired();

                // One Executor assignment per Will (whole-Will scope).
                entity.HasIndex(x => x.WillId).IsUnique();

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

                entity.Property(x => x.GraceDays)
                    .HasDefaultValue(2);

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
            // HANDOVER / LEGAL / WALLET / REVIEWS
            // =========================
            modelBuilder.Entity<PaymentDispute>(entity =>
            {
                entity.ToTable("PaymentDisputes");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.WillId).IsUnique();
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => new { x.RaisedByAccountId, x.CreatedAt });
                entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
                entity.Property(x => x.ResolutionAction).HasMaxLength(20);
                entity.Property(x => x.ResolutionNote).HasMaxLength(1000);
                entity.HasOne<Will>().WithMany().HasForeignKey(x => x.WillId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
                entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Details).HasMaxLength(2000);
                entity.HasIndex(x => x.CreatedAt);
                entity.HasIndex(x => new { x.ActorAccountId, x.CreatedAt });
                entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
            });

            modelBuilder.Entity<ExecutorReview>(entity =>
            {
                entity.ToTable("ExecutorReviews");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.WillId).IsUnique();
                entity.HasIndex(x => x.ExecutorAccountId);
                entity.Property(x => x.Comment).HasMaxLength(1000);
                entity.HasOne(x => x.OwnerAccount)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LegalCase>(entity =>
            {
                entity.ToTable("LegalCases");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.WillId);
                entity.HasIndex(x => x.Status);
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x => x.FilePath).IsRequired();
                entity.HasOne(x => x.Will)
                    .WithMany()
                    .HasForeignKey(x => x.WillId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ExecutorAccount)
                    .WithMany()
                    .HasForeignKey(x => x.ExecutorAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HandoverCase>(entity =>
            {
                entity.ToTable("HandoverCases");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.WillId);
                entity.Property(x => x.Type).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.HasOne(x => x.Will)
                    .WithMany()
                    .HasForeignKey(x => x.WillId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HandoverItem>(entity =>
            {
                entity.ToTable("HandoverItems");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.HandoverCaseId, x.AssetAssignmentId })
                    .IsUnique();
                entity.Property(x => x.Status).IsRequired();
                entity.HasOne(x => x.HandoverCase)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.HandoverCaseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.AssetAssignment)
                    .WithMany()
                    .HasForeignKey(x => x.AssetAssignmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallets");
                entity.HasKey(x => x.AccountId);
                entity.Property(x => x.Balance).HasPrecision(18, 2);
                entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            });

            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.ToTable("WalletTransactions");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.AccountId, x.CreatedAt });
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.BalanceAfter).HasPrecision(18, 2);
                entity.Property(x => x.Type).IsRequired();
                entity.Property(x => x.IdempotencyKey).HasMaxLength(100);
                entity.HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique()
                    .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            });

            modelBuilder.Entity<ServicePayment>(entity =>
            {
                entity.ToTable("ServicePayments");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.WillId).IsUnique();
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.HasOne(x => x.Will)
                    .WithMany()
                    .HasForeignKey(x => x.WillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // IDP USER
            // =========================
            modelBuilder.Entity<IdpUser>(entity =>
            {
                entity.ToTable("IdpUsers","public");

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
                entity.ToTable("Faces", "public");

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
