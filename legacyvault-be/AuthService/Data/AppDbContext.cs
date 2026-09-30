using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>(e =>
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
            e.HasIndex(a => a.Email).IsUnique();
            e.HasIndex(a => a.Phone).IsUnique();
        });
    }

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
