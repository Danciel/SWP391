using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Contracts;

namespace SWP301BETASK.Features.WillManagement.Services
{
    /// <summary>Read-only access to Accounts (IdentityAccess owns the table).</summary>
    internal sealed class AccountLookup : IAccountLookup
    {
        // Account.IdentityStatus defaults to "UNVERIFIED". Confirm with Nhiên that the verified value is "VERIFIED".
        private const string Verified = "VERIFIED";

        private readonly LegacyVaultDbContext _db;
        public AccountLookup(LegacyVaultDbContext db) => _db = db;

        public Task<AccountInfo?> GetAsync(Guid accountId, CancellationToken ct = default) =>
            _db.Accounts.AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => new AccountInfo(a.Id, a.FullName, a.Email, a.Phone))
                .FirstOrDefaultAsync(ct);

        public Task<bool> IsIdentityVerifiedAsync(Guid accountId, CancellationToken ct = default) =>
            _db.Accounts.AsNoTracking().AnyAsync(a => a.Id == accountId && a.IdentityStatus == Verified, ct);
    }
}
