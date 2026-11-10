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

        public async Task ConfirmByExecutorAsync(
            Guid willId,
            Guid designationId,
            Guid executorAccountId,
            string note,
            CancellationToken cancellationToken = default)
        {
            var isExecutor = await _db.ExecutorAssignments.AnyAsync(x =>
                x.WillId == willId &&
                x.ExecutorAccountId == executorAccountId &&
                x.Status == "ACCEPTED", cancellationToken);

            if (!isExecutor)
                throw new KeyNotFoundException("Handover not found.");

            var running = await _db.HandoverCases.AnyAsync(c =>
                c.WillId == willId &&
                (c.Status == "IN_PROGRESS" || c.Status == "PAUSED"), cancellationToken);

            if (!running)
            {
                throw new WillConflictException(
                    "The handover is not running, so a beneficiary cannot be linked.");
            }

            var cleanNote = (note ?? string.Empty).Trim();
            if (cleanNote.Length < 5)
            {
                throw new InvalidOperationException(
                    "Write why you confirm this person (at least 5 characters).");
            }

            var designation = await _db.BeneficiaryDesignations
                .FirstOrDefaultAsync(x =>
                    x.Id == designationId && x.WillId == willId, cancellationToken)
                ?? throw new KeyNotFoundException("Beneficiary not found.");

            if (designation.BeneficiaryAccountId != null)
                throw new InvalidOperationException(
                    "This beneficiary is already linked to an account.");

            var email = designation.Email.Trim().ToLower();

            var account = await _db.Accounts.AsNoTracking()
                .Where(x => x.EmailVerified && x.Email.ToLower() == email)
                .Select(x => new { x.Id, x.FullName })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "No account with this verified email exists yet.");

            // The Executor must not link themselves or the Owner as a beneficiary.
            var ownerId = await _db.Wills.AsNoTracking()
                .Where(x => x.Id == willId)
                .Select(x => x.OwnerAccountId)
                .FirstAsync(cancellationToken);

            if (account.Id == executorAccountId || account.Id == ownerId)
            {
                throw new InvalidOperationException(
                    "This account cannot be linked as a beneficiary of this will.");
            }

            var now = DateTimeOffset.UtcNow;
            designation.BeneficiaryAccountId = account.Id;
            designation.UpdatedAt = now;

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = executorAccountId,
                Action = "BENEFICIARY_LINKED_BY_EXECUTOR",
                EntityType = "BeneficiaryDesignation",
                EntityId = designation.Id,
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    willId,
                    accountId = account.Id,
                    designationName = designation.FullName,
                    accountName = account.FullName,
                    namesMatch = NamesMatch(account.FullName, designation.FullName),
                    note = cleanNote
                }),
                CreatedAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);

            // Assets already delivered to this person? Their access code goes out now.
            await _codes.IssueForWillAsync(willId, cancellationToken);
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
