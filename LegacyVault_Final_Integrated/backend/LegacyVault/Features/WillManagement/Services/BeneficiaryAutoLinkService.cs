using System.Globalization;
using System.Text;
using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // Rules (same proof the owner's "link" button asks for, minus the owner):
    //   * the account email is VERIFIED and equals the designation email;
    //   * the account name equals the designation name (ignoring case, accents,
    //     extra spaces). A different name is NOT linked automatically.
    // Only designations of wills with a running handover are touched.
    public class BeneficiaryAutoLinkService : IBeneficiaryAutoLinkService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly IHandoverCodeService _codes;
        private readonly ILogger<BeneficiaryAutoLinkService> _logger;

        public BeneficiaryAutoLinkService(
            LegacyVaultDbContext db,
            IHandoverCodeService codes,
            ILogger<BeneficiaryAutoLinkService> logger)
        {
            _db = db;
            _codes = codes;
            _logger = logger;
        }

        public async Task<int> TryLinkAccountAsync(
            Guid accountId, CancellationToken cancellationToken = default)
        {
            var account = await _db.Accounts.AsNoTracking()
                .Where(x => x.Id == accountId)
                .Select(x => new { x.Id, x.Email, x.FullName, x.EmailVerified })
                .FirstOrDefaultAsync(cancellationToken);

            if (account == null || !account.EmailVerified) return 0;

            var email = account.Email.Trim().ToLower();

            var candidates = await _db.BeneficiaryDesignations
                .Where(x => x.BeneficiaryAccountId == null &&
                            x.Email.ToLower() == email &&
                            _db.HandoverCases.Any(c =>
                                c.WillId == x.WillId &&
                                (c.Status == "IN_PROGRESS" || c.Status == "PAUSED")))
                .ToListAsync(cancellationToken);

            return await LinkAsync(account.Id, account.FullName, candidates, cancellationToken);
        }

        public async Task<int> TryLinkWillAsync(
            Guid willId, CancellationToken cancellationToken = default)
        {
            var unlinked = await _db.BeneficiaryDesignations
                .Where(x => x.WillId == willId && x.BeneficiaryAccountId == null)
                .ToListAsync(cancellationToken);

            var linked = 0;

            foreach (var designation in unlinked)
            {
                var email = designation.Email.Trim().ToLower();

                var account = await _db.Accounts.AsNoTracking()
                    .Where(x => x.EmailVerified && x.Email.ToLower() == email)
                    .Select(x => new { x.Id, x.FullName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (account == null) continue;

                linked += await LinkAsync(
                    account.Id, account.FullName,
                    new List<BeneficiaryDesignation> { designation },
                    cancellationToken);
            }

            return linked;
        }

        private async Task<int> LinkAsync(
            Guid accountId, string accountName,
            List<BeneficiaryDesignation> designations,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var willIds = new HashSet<Guid>();

            foreach (var d in designations)
            {
                if (!NamesMatch(accountName, d.FullName))
                {
                    _logger.LogInformation(
                        "Beneficiary {DesignationId} was not auto-linked: the account name differs.",
                        d.Id);
                    continue;
                }

                d.BeneficiaryAccountId = accountId;
                d.UpdatedAt = now;
                willIds.Add(d.WillId);

                _db.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActorAccountId = accountId,
                    Action = "BENEFICIARY_AUTO_LINKED",
                    EntityType = "BeneficiaryDesignation",
                    EntityId = d.Id,
                    Details = System.Text.Json.JsonSerializer.Serialize(new { d.WillId }),
                    CreatedAt = now
                });
            }

            if (willIds.Count == 0) return 0;

            await _db.SaveChangesAsync(cancellationToken);

            // Assets already delivered to this person? Their access code goes out now.
            foreach (var willId in willIds)
                await _codes.IssueForWillAsync(willId, cancellationToken);

            return willIds.Count;
        }

        // "Nguyễn  Văn A" == "nguyen van a"
        internal static bool NamesMatch(string? a, string? b)
        {
            var left = NormalizeName(a);
            return left.Length > 0 && left == NormalizeName(b);
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var decomposed = name.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;

                sb.Append((c == 'đ' || c == 'Đ') ? 'd' : char.ToLowerInvariant(c));
            }

            return string.Join(' ',
                sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
