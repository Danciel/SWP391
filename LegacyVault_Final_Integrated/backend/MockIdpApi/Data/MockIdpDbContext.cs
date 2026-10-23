using Microsoft.EntityFrameworkCore;
using MockIdpApi.Models; // Adjust namespace based on your new project name

namespace MockIdpApi.Data
{
    public class MockIdpDbContext : DbContext
    {
        public MockIdpDbContext(DbContextOptions<MockIdpDbContext> options) : base(options) { }

        public DbSet<IdpUser> IdpUsers { get; set; }
        public DbSet<Face> Faces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdpUser>(entity =>
            {
                entity.ToTable("IdpUsers", "public");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
                entity.Property(x => x.Phone).HasMaxLength(32);
                entity.Property(x => x.IdentityStatus).HasMaxLength(32).IsRequired();
                entity.Property(x => x.LifeStatus).HasMaxLength(32); // Added from LV-12
                entity.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<Face>(entity =>
            {
                entity.ToTable("Faces", "public");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.EmbeddingJson).IsRequired();
                entity.HasOne(x => x.IdpUser)
                      .WithOne()
                      .HasForeignKey<Face>(x => x.IdpUserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}