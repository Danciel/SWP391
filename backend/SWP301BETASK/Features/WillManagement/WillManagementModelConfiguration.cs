using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Features.WillManagement.Models;

namespace SWP301BETASK.Features.WillManagement
{
    /// <summary>
    /// Entity configuration for the WillManagement tables.
    /// LegacyVaultDbContext.OnModelCreating only needs: modelBuilder.ConfigureWillManagement();
    /// No FKs to Accounts/Assets on purpose (other modules own them) - only indexes.
    /// </summary>
    public static class WillManagementModelConfiguration
    {
        public static readonly Guid ExecDavid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ExecLinh = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ExecKhoa = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public static void ConfigureWillManagement(this ModelBuilder mb)
        {
            mb.Entity<Will>(e =>
            {
                e.ToTable("Wills");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever(); // client-generated Guids: EF must treat new children as Added
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(2000);
                e.Property(x => x.Status).HasMaxLength(40);
                e.HasIndex(x => x.OwnerId);
                e.HasMany(x => x.Beneficiaries).WithOne(b => b.Will).HasForeignKey(b => b.WillId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Assignments).WithOne(a => a.Will).HasForeignKey(a => a.WillId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Executor).WithOne(a => a.Will).HasForeignKey<ExecutorAssignment>(a => a.WillId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Dms).WithOne(d => d.Will).HasForeignKey<DmsConfiguration>(d => d.WillId).OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<BeneficiaryDesignation>(e =>
            {
                e.ToTable("BeneficiaryDesignations");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.FullName).HasMaxLength(200);
                e.Property(x => x.Email).HasMaxLength(256);
                e.Property(x => x.Phone).HasMaxLength(30);
                e.Property(x => x.IdentityNumber).HasMaxLength(20);
                e.Property(x => x.Relationship).HasMaxLength(50);
                e.HasIndex(x => x.BeneficiaryAccountId);
            });

            mb.Entity<AssetAssignment>(e =>
            {
                e.ToTable("AssetAssignments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.AssignmentStatus).HasMaxLength(20);
                e.Property(x => x.HandoverStatus).HasMaxLength(20);
                e.HasIndex(x => new { x.WillId, x.AssetId }).IsUnique();
                e.HasOne(x => x.Beneficiary).WithMany().HasForeignKey(x => x.BeneficiaryDesignationId).OnDelete(DeleteBehavior.SetNull);
            });

            mb.Entity<ExecutorProfile>(e =>
            {
                e.ToTable("ExecutorProfiles");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.Specialization).HasMaxLength(200);
                e.Property(x => x.License).HasMaxLength(50);
                e.Property(x => x.Fee).HasPrecision(18, 0);
                e.HasData(
                    new ExecutorProfile { Id = ExecDavid, Name = "David Nguyen", Specialization = "Digital Estate Specialist", License = "LAW-2026-00182", Rating = 4.9, Reviews = 128, ExperienceYears = 8, CompletedCases = 86, Fee = 3000000, Available = true },
                    new ExecutorProfile { Id = ExecLinh, Name = "Linh Tran", Specialization = "Crypto Asset Specialist", License = "LAW-2026-00219", Rating = 4.8, Reviews = 94, ExperienceYears = 6, CompletedCases = 61, Fee = 2500000, Available = true },
                    new ExecutorProfile { Id = ExecKhoa, Name = "Khoa Pham", Specialization = "Family Legacy Attorney", License = "LAW-2025-00087", Rating = 4.7, Reviews = 152, ExperienceYears = 11, CompletedCases = 140, Fee = 3500000, Available = true });
            });

            mb.Entity<ExecutorAssignment>(e =>
            {
                e.ToTable("ExecutorAssignments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.Status).HasMaxLength(20);
                e.Property(x => x.PaymentStatus).HasMaxLength(20);
                e.Property(x => x.Fee).HasPrecision(18, 0);
                e.HasIndex(x => x.WillId).IsUnique();
                e.HasOne(x => x.Profile).WithMany().HasForeignKey(x => x.ExecutorProfileId).OnDelete(DeleteBehavior.Restrict);
            });

            mb.Entity<DmsConfiguration>(e =>
            {
                e.ToTable("DmsConfigurations");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.Status).HasMaxLength(20);
                e.HasIndex(x => x.WillId).IsUnique();
            });
        }
    }
}
