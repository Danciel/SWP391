using System.Security.Cryptography;
using System.Text;
using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // Per-recipient handover access code.
    //   * Format XXXX-XXXX-XXXX: 11 random symbols + 1 check symbol, so a typo is
    //     rejected without using up one of the 3 attempts (~54 bits of randomness).
    //   * Generated when the Executor delivers, emailed to the recipient's verified
    //     account email. Only a salted + peppered hash is stored: nobody (owner,
    //     Executor, admin, database reader) can read the code back.
    //   * 3 wrong codes lock it for 15 minutes; the recipient and the owner are emailed.
    public class HandoverCodeService : IHandoverCodeService
    {
        // No 0/O and 1/I/L: they are easy to confuse when read or typed.
        private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        private const int CodeLength = 12;

        private const int MaxAttempts = 3;
        private const int LockMinutes = 15;
        private const int ResendCooldownMinutes = 5;
        private const int MaxIssues = 5;
        private const int Iterations = 100_000;

        private readonly LegacyVaultDbContext _db;
        private readonly IEmailSender _email;
        private readonly IConfiguration _config;
        private readonly ILogger<HandoverCodeService> _logger;

        public HandoverCodeService(
            LegacyVaultDbContext db,
            IEmailSender email,
            IConfiguration config,
            ILogger<HandoverCodeService> logger)
        {
            _db = db;
            _email = email;
            _config = config;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Issue
        // ------------------------------------------------------------------
        public async Task IssueForWillAsync(Guid willId, CancellationToken cancellationToken = default)
        {
            try
            {
                var caseId = await _db.HandoverCases.AsNoTracking()
                    .Where(x => x.WillId == willId &&
                                (x.Status == "IN_PROGRESS" || x.Status == "COMPLETED"))
                    .OrderByDescending(x => x.StartedAt)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (caseId != null)
                    await IssueForCaseAsync(caseId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Issuing handover access codes failed for will {WillId}.", willId);
            }
        }

        public async Task IssueForCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
        {
            try
            {
                var info = await _db.HandoverCases.AsNoTracking()
                    .Where(x => x.Id == caseId)
                    .Select(x => new
                    {
                        x.Status,
                        OwnerName = x.Will!.OwnerAccount != null
                            ? x.Will.OwnerAccount.FullName
                            : string.Empty
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (info == null ||
                    (info.Status != "IN_PROGRESS" && info.Status != "COMPLETED"))
                {
                    return;
                }

                // Recipients that already have delivered assets and a linked account.
                var recipients = await _db.HandoverItems.AsNoTracking()
                    .Where(i => i.HandoverCaseId == caseId &&
                                i.Status == "DELIVERED" &&
                                i.AssetAssignment!.BeneficiaryDesignation!
                                    .BeneficiaryAccountId != null)
                    .Select(i => new
                    {
                        DesignationId = i.AssetAssignment!.BeneficiaryDesignationId,
                        AccountId = i.AssetAssignment.BeneficiaryDesignation!
                            .BeneficiaryAccountId
                    })
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var r in recipients)
                {
                    if (r.AccountId == null) continue;

                    var exists = await _db.HandoverAccessCodes.AnyAsync(x =>
                        x.HandoverCaseId == caseId &&
                        x.BeneficiaryDesignationId == r.DesignationId,
                        cancellationToken);

                    if (exists) continue;

                    await IssueAsync(
                        caseId, r.DesignationId, r.AccountId.Value,
                        info.OwnerName, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Never break a delivery because of the code.
                _logger.LogWarning(ex, "Issuing handover access codes failed for case {CaseId}.", caseId);
            }
        }

        private async Task<bool> IssueAsync(
            Guid caseId, Guid designationId, Guid accountId,
            string ownerName, CancellationToken cancellationToken)
        {
            var code = GenerateCode();
            var salt = RandomNumberGenerator.GetBytes(16);
            var now = DateTimeOffset.UtcNow;

            var row = new HandoverAccessCode
            {
                Id = Guid.NewGuid(),
                HandoverCaseId = caseId,
                BeneficiaryDesignationId = designationId,
                Salt = salt,
                CodeHash = Hash(StripDashes(code), salt, caseId, designationId),
                IssuedAt = now,
                LastIssuedAt = now,
                IssueCount = 1
            };

            // The audit row never contains the code.
            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = null,
                Action = "HANDOVER_CODE_ISSUED",
                EntityType = "HandoverAccessCode",
                EntityId = row.Id,
                Details = System.Text.Json.JsonSerializer.Serialize(
                    new { caseId, designationId }),
                CreatedAt = now
            };

            _db.HandoverAccessCodes.Add(row);
            _db.AuditLogs.Add(audit);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another request issued this recipient's code first.
                _db.Entry(row).State = EntityState.Detached;
                _db.Entry(audit).State = EntityState.Detached;
                return false;
            }

            await SendCodeEmailAsync(row.Id, accountId, ownerName, code, cancellationToken);
            return true;
        }

        private async Task SendCodeEmailAsync(
            Guid rowId, Guid accountId, string ownerName,
            string code, CancellationToken cancellationToken)
        {
            try
            {
                var account = await _db.Accounts.AsNoTracking()
                    .Where(x => x.Id == accountId)
                    .Select(x => new { x.Email, x.FullName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (account == null) return;

                await _email.SendHandoverCodeEmailAsync(
                    account.Email, account.FullName, ownerName, code);

                await _db.HandoverAccessCodes
                    .Where(x => x.Id == rowId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.EmailedAt, DateTimeOffset.UtcNow),
                        cancellationToken);
            }
            catch (Exception ex)
            {
                // The recipient can press "Send again" (a new code replaces this one).
                _logger.LogWarning(ex,
                    "Could not email the handover access code (row {RowId}).", rowId);
            }
        }

        // ------------------------------------------------------------------
        // Verify
        // ------------------------------------------------------------------
        public async Task<AccessCodeResult> VerifyAsync(
            Guid caseId, Guid accountId, string? code,
            CancellationToken cancellationToken = default)
        {
            var designationId = await FindDesignationIdAsync(caseId, accountId, cancellationToken);
            if (designationId == null)
                return Fail(404, "Handover not found.");

            var caseStatus = await _db.HandoverCases.AsNoTracking()
                .Where(x => x.Id == caseId)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (caseStatus != "IN_PROGRESS" && caseStatus != "COMPLETED")
                return Fail(409, "This handover is not active.");

            // A typo is rejected before it can use up one of the 3 attempts.
            if (!TryNormalize(code, out var normalized))
            {
                return Fail(400,
                    "This is not a valid access code. Check it and try again (format XXXX-XXXX-XXXX).");
            }

            var row = await _db.HandoverAccessCodes.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.HandoverCaseId == caseId &&
                    x.BeneficiaryDesignationId == designationId.Value,
                    cancellationToken);

            if (row == null)
            {
                // Handover started before codes existed, or the account was linked late.
                await IssueForCaseAsync(caseId, cancellationToken);

                var issued = await _db.HandoverAccessCodes.AsNoTracking()
                    .AnyAsync(x =>
                        x.HandoverCaseId == caseId &&
                        x.BeneficiaryDesignationId == designationId.Value,
                        cancellationToken);

                return issued
                    ? Fail(409, "Your access code has just been sent to your email.")
                    : Fail(409, "The Executor has not delivered your assets yet.");
            }

            var now = DateTimeOffset.UtcNow;

            // 1. A finished lock starts a fresh count.
            await _db.HandoverAccessCodes
                .Where(x => x.Id == row.Id && x.LockedUntil != null && x.LockedUntil <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.FailedAttempts, 0)
                    .SetProperty(x => x.LockedUntil, (DateTimeOffset?)null),
                    cancellationToken);

            // 2. Reserve one attempt atomically BEFORE comparing, so parallel
            //    requests cannot exceed the limit.
            var reserved = await _db.HandoverAccessCodes
                .Where(x => x.Id == row.Id &&
                            x.LockedUntil == null &&
                            x.FailedAttempts < MaxAttempts)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.FailedAttempts, x => x.FailedAttempts + 1),
                    cancellationToken);

            if (reserved == 0)
            {
                var lockedUntil = await _db.HandoverAccessCodes.AsNoTracking()
                    .Where(x => x.Id == row.Id)
                    .Select(x => x.LockedUntil)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lockedUntil == null)
                {
                    // Attempts used up but no lock time was written: write it now.
                    lockedUntil = now.AddMinutes(LockMinutes);

                    await _db.HandoverAccessCodes
                        .Where(x => x.Id == row.Id && x.LockedUntil == null)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.LockedUntil, lockedUntil),
                            cancellationToken);
                }

                return Locked(lockedUntil.Value);
            }

            // 3. Compare in constant time.
            var candidate = Hash(normalized, row.Salt, caseId, designationId.Value);
            var isCorrect = CryptographicOperations.FixedTimeEquals(candidate, row.CodeHash);
            CryptographicOperations.ZeroMemory(candidate);

            if (isCorrect)
            {
                await _db.HandoverAccessCodes
                    .Where(x => x.Id == row.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.FailedAttempts, 0)
                        .SetProperty(x => x.LockedUntil, (DateTimeOffset?)null)
                        .SetProperty(x => x.VerifiedAt, now),
                        cancellationToken);

                _db.AuditLogs.Add(NewAudit(accountId, "HANDOVER_CODE_VERIFIED", row.Id,
                    new { caseId }));
                await _db.SaveChangesAsync(cancellationToken);

                return new AccessCodeResult(true, 200, "Access code confirmed.");
            }

            var attempts = await _db.HandoverAccessCodes.AsNoTracking()
                .Where(x => x.Id == row.Id)
                .Select(x => x.FailedAttempts)
                .FirstOrDefaultAsync(cancellationToken);

            var remaining = Math.Max(0, MaxAttempts - attempts);

            if (remaining == 0)
            {
                var until = now.AddMinutes(LockMinutes);

                // Only the request that sets the lock sends the emails.
                var lockedRows = await _db.HandoverAccessCodes
                    .Where(x => x.Id == row.Id && x.LockedUntil == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.LockedUntil, until),
                        cancellationToken);

                if (lockedRows == 1)
                {
                    _db.AuditLogs.Add(NewAudit(accountId, "HANDOVER_CODE_LOCKED", row.Id,
                        new { caseId }));
                    await _db.SaveChangesAsync(cancellationToken);

                    await NotifyLockedAsync(caseId, accountId, until, cancellationToken);
                }

                return Locked(until);
            }

            return new AccessCodeResult(false, 400,
                $"Incorrect access code. {remaining} attempt{(remaining == 1 ? "" : "s")} left.",
                remaining);
        }

        private static AccessCodeResult Locked(DateTimeOffset until) =>
            new(false, 429,
                $"Too many wrong codes. Locked for {LockMinutes} minutes.",
                0, until);

        // ------------------------------------------------------------------
        // Resend: a NEW code replaces the old one
        // ------------------------------------------------------------------
        public async Task<AccessCodeResult> ResendAsync(
            Guid caseId, Guid accountId,
            CancellationToken cancellationToken = default)
        {
            var designationId = await FindDesignationIdAsync(caseId, accountId, cancellationToken);
            if (designationId == null)
                return Fail(404, "Handover not found.");

            var info = await _db.HandoverCases.AsNoTracking()
                .Where(x => x.Id == caseId)
                .Select(x => new
                {
                    x.Status,
                    OwnerName = x.Will!.OwnerAccount != null
                        ? x.Will.OwnerAccount.FullName
                        : string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (info == null ||
                (info.Status != "IN_PROGRESS" && info.Status != "COMPLETED"))
            {
                return Fail(409, "This handover is not active.");
            }

            var delivered = await _db.HandoverItems.AnyAsync(i =>
                i.HandoverCaseId == caseId &&
                i.Status == "DELIVERED" &&
                i.AssetAssignment!.BeneficiaryDesignationId == designationId.Value,
                cancellationToken);

            if (!delivered)
                return Fail(409, "The Executor has not delivered your assets yet.");

            var row = await _db.HandoverAccessCodes.FirstOrDefaultAsync(x =>
                x.HandoverCaseId == caseId &&
                x.BeneficiaryDesignationId == designationId.Value,
                cancellationToken);

            if (row == null)
            {
                await IssueForCaseAsync(caseId, cancellationToken);
                return new AccessCodeResult(true, 200, "Your access code was sent to your email.");
            }

            var now = DateTimeOffset.UtcNow;

            // Confirmed once = confirmed for good (a new handover gets new codes).
            if (row.VerifiedAt != null)
                return Fail(409, "Your access code is already confirmed.");

            if (row.LockedUntil != null && row.LockedUntil.Value > now)
                return Locked(row.LockedUntil.Value);

            if (row.LastIssuedAt.AddMinutes(ResendCooldownMinutes) > now)
            {
                return Fail(429,
                    $"Please wait {ResendCooldownMinutes} minutes before asking for another code.");
            }

            if (row.IssueCount >= MaxIssues)
            {
                return Fail(429,
                    "The limit of new codes was reached. Ask your Executor or the support team for help.");
            }

            var code = GenerateCode();
            var salt = RandomNumberGenerator.GetBytes(16);

            row.Salt = salt;
            row.CodeHash = Hash(StripDashes(code), salt, caseId, designationId.Value);
            row.LastIssuedAt = now;
            row.IssueCount += 1;
            row.FailedAttempts = 0;
            row.LockedUntil = null;
            row.VerifiedAt = null;
            row.EmailedAt = null;

            _db.AuditLogs.Add(NewAudit(accountId, "HANDOVER_CODE_RESENT", row.Id,
                new { caseId, issueCount = row.IssueCount }));
            await _db.SaveChangesAsync(cancellationToken);

            await SendCodeEmailAsync(row.Id, accountId, info.OwnerName, code, cancellationToken);

            return new AccessCodeResult(true, 200,
                "A new access code was sent to your email. The old one no longer works.");
        }

        // ------------------------------------------------------------------
        // Status
        // ------------------------------------------------------------------
        public async Task<AccessCodeStatus> GetStatusAsync(
            Guid caseId, Guid designationId,
            CancellationToken cancellationToken = default)
        {
            var row = await _db.HandoverAccessCodes.AsNoTracking()
                .Where(x => x.HandoverCaseId == caseId &&
                            x.BeneficiaryDesignationId == designationId)
                .Select(x => new { x.LockedUntil, x.VerifiedAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (row == null)
                return new AccessCodeStatus("NOT_ISSUED", false, null, null);

            var now = DateTimeOffset.UtcNow;

            // The code only proves "you received the email": once it was typed
            // correctly it stays confirmed. PIN + face scan still guard everything else.
            if (row.VerifiedAt != null)
                return new AccessCodeStatus("VERIFIED", true, null, null);

            if (row.LockedUntil != null && row.LockedUntil.Value > now)
                return new AccessCodeStatus("LOCKED", false, row.LockedUntil, null);

            return new AccessCodeStatus("SENT", false, null, null);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static AccessCodeResult Fail(int statusCode, string message) =>
            new(false, statusCode, message);

        private static AuditLog NewAudit(Guid actor, string action, Guid entityId, object details) =>
            new()
            {
                Id = Guid.NewGuid(),
                ActorAccountId = actor,
                Action = action,
                EntityType = "HandoverAccessCode",
                EntityId = entityId,
                Details = System.Text.Json.JsonSerializer.Serialize(details),
                CreatedAt = DateTimeOffset.UtcNow
            };

        private Task<Guid?> FindDesignationIdAsync(
            Guid caseId, Guid accountId, CancellationToken cancellationToken) =>
            _db.HandoverItems.AsNoTracking()
                .Where(i => i.HandoverCaseId == caseId &&
                            i.AssetAssignment!.BeneficiaryDesignation!
                                .BeneficiaryAccountId == accountId)
                .Select(i => (Guid?)i.AssetAssignment!.BeneficiaryDesignationId)
                .FirstOrDefaultAsync(cancellationToken);

        // Tells the recipient (and the owner) that the code is locked.
        private async Task NotifyLockedAsync(
            Guid caseId, Guid accountId, DateTimeOffset until,
            CancellationToken cancellationToken)
        {
            try
            {
                var recipient = await _db.Accounts.AsNoTracking()
                    .Where(x => x.Id == accountId)
                    .Select(x => new { x.Email, x.FullName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (recipient == null) return;

                await _email.SendHandoverCodeLockedEmailAsync(
                    recipient.Email, recipient.FullName, recipient.FullName,
                    until, toOwner: false);

                var ownerEmail = await _db.HandoverCases.AsNoTracking()
                    .Where(x => x.Id == caseId)
                    .Select(x => x.Will!.OwnerAccount!.Email)
                    .FirstOrDefaultAsync(cancellationToken);

                var ownerName = await _db.HandoverCases.AsNoTracking()
                    .Where(x => x.Id == caseId)
                    .Select(x => x.Will!.OwnerAccount!.FullName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(ownerEmail))
                {
                    await _email.SendHandoverCodeLockedEmailAsync(
                        ownerEmail, ownerName ?? string.Empty, recipient.FullName,
                        until, toOwner: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send the access code lock emails for case {CaseId}.", caseId);
            }
        }

        // ---- code format -------------------------------------------------
        internal static string GenerateCode()
        {
            var chars = new char[CodeLength];
            var sum = 0;

            for (var i = 0; i < CodeLength - 1; i++)
            {
                var v = RandomNumberGenerator.GetInt32(Alphabet.Length);
                chars[i] = Alphabet[v];
                sum += (i + 1) * v;
            }

            chars[CodeLength - 1] = Alphabet[sum % Alphabet.Length];

            return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 4)}";
        }

        private static string StripDashes(string code) => code.Replace("-", "");

        // Upper-cases, drops dashes/spaces and checks length, symbols and the check symbol.
        internal static bool TryNormalize(string? input, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var sb = new StringBuilder(CodeLength);
            foreach (var ch in input)
            {
                if (ch == '-' || char.IsWhiteSpace(ch)) continue;
                sb.Append(char.ToUpperInvariant(ch));
            }

            if (sb.Length != CodeLength) return false;

            var candidate = sb.ToString();
            var sum = 0;

            for (var i = 0; i < CodeLength - 1; i++)
            {
                var v = Alphabet.IndexOf(candidate[i]);
                if (v < 0) return false;
                sum += (i + 1) * v;
            }

            var check = Alphabet.IndexOf(candidate[CodeLength - 1]);
            if (check < 0 || check != sum % Alphabet.Length) return false;

            normalized = candidate;
            return true;
        }

        // HMAC(pepper, context + code) -> PBKDF2(salt). Same pepper setting as the Security PIN.
        private byte[] Hash(string normalizedCode, byte[] salt, Guid caseId, Guid designationId)
        {
            var pepper = _config["OwnerPin:Pepper"]
                ?? throw new InvalidOperationException("OwnerPin:Pepper is missing.");

            if (pepper.Length < 32)
                throw new InvalidOperationException("Owner PIN pepper is too short.");

            var keyed = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(pepper),
                Encoding.UTF8.GetBytes(
                    $"handover-code|{caseId:N}|{designationId:N}|{normalizedCode}"));

            try
            {
                return Rfc2898DeriveBytes.Pbkdf2(
                    keyed, salt, Iterations, HashAlgorithmName.SHA256, 32);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyed);
            }
        }
    }
}
