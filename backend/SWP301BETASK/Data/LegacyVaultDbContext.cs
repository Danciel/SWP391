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
    }
}
