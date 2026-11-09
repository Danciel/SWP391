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

        // Executor "send a new code" is not limited by the recipient's 5 codes,
        // but it keeps the cooldown and has a hard ceiling.
        private const int MaxIssuesByExecutor = 10;

        private const string NotConfiguredMessage =
            "Access codes are temporarily unavailable because of a server setting. Please contact support.";

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

        public bool IsConfigured
        {
            get
            {
                var pepper = _config["OwnerPin:Pepper"];
                return !string.IsNullOrWhiteSpace(pepper) && pepper.Length >= 32;
            }
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
            if (!IsConfigured)
            {
                _logger.LogError(
                    "Handover access codes cannot be issued: OwnerPin:Pepper is missing or too short (case {CaseId}).",
                    caseId);
                return;
            }

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

            if (!IsConfigured)
                return Fail(503, NotConfiguredMessage);

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

                if (issued)
                    return Fail(409, "Your access code has just been sent to your email.");

                var deliveredToMe = await _db.HandoverItems.AnyAsync(i =>
                    i.HandoverCaseId == caseId &&
                    i.Status == "DELIVERED" &&
                    i.AssetAssignment!.BeneficiaryDesignationId == designationId.Value,
                    cancellationToken);

                return deliveredToMe
                    ? Fail(503, "We could not issue your access code right now. Try again in a few minutes or contact support.")
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

            if (!IsConfigured)
                return Fail(503, NotConfiguredMessage);

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

                var issuedNow = await _db.HandoverAccessCodes.AsNoTracking().AnyAsync(x =>
                    x.HandoverCaseId == caseId &&
                    x.BeneficiaryDesignationId == designationId.Value,
                    cancellationToken);

                return issuedNow
                    ? new AccessCodeResult(true, 200, "Your access code was sent to your email.")
                    : Fail(503, "We could not issue your access code right now. Try again in a few minutes or contact support.");
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
                    "The limit of new codes was reached. Use \"Ask for help\": your Executor and the support team will be told.");
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
                .Select(x => new
                {
                    x.Id, x.LockedUntil, x.VerifiedAt, x.IssueCount, x.LastIssuedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (row == null)
                return new AccessCodeStatus("NOT_ISSUED", false, null, null);

            var now = DateTimeOffset.UtcNow;

            // The code only proves "you received the email": once it was typed
            // correctly it stays confirmed. PIN + face scan still guard everything else.
            if (row.VerifiedAt != null)
                return new AccessCodeStatus("VERIFIED", true, null, null, row.IssueCount, MaxIssues);

            var support = await HasRecentSupportRequestAsync(row.Id, row.LastIssuedAt, cancellationToken);
            var nextResend = row.LastIssuedAt.AddMinutes(ResendCooldownMinutes);

            if (row.LockedUntil != null && row.LockedUntil.Value > now)
            {
                return new AccessCodeStatus(
                    "LOCKED", false, row.LockedUntil, null,
                    row.IssueCount, MaxIssues, nextResend, support);
            }

            return new AccessCodeStatus(
                "SENT", false, null, null,
                row.IssueCount, MaxIssues, nextResend, support);
        }

        private Task<bool> HasRecentSupportRequestAsync(
            Guid rowId, DateTimeOffset since, CancellationToken cancellationToken) =>
            _db.AuditLogs.AsNoTracking().AnyAsync(a =>
                a.Action == "HANDOVER_CODE_SUPPORT_REQUESTED" &&
                a.EntityId == rowId &&
                a.CreatedAt >= since,
                cancellationToken);

        // ------------------------------------------------------------------
        // Executor: send a new code to one recipient
        // ------------------------------------------------------------------
        public async Task<AccessCodeResult> ResendByExecutorAsync(
            Guid willId, Guid designationId, Guid executorAccountId,
            CancellationToken cancellationToken = default)
        {
            var isExecutor = await _db.ExecutorAssignments.AnyAsync(x =>
                x.WillId == willId &&
                x.ExecutorAccountId == executorAccountId &&
                x.Status == "ACCEPTED",
                cancellationToken);

            if (!isExecutor)
                return Fail(404, "Handover not found.");

            if (await _db.ExecutorProfiles.AnyAsync(x =>
                    x.AccountId == executorAccountId && x.IsSuspended, cancellationToken))
            {
                return Fail(403, "Suspended executors cannot send access codes.");
            }

            var info = await _db.HandoverCases.AsNoTracking()
                .Where(x => x.WillId == willId &&
                            (x.Status == "IN_PROGRESS" || x.Status == "COMPLETED"))
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new
                {
                    x.Id,
                    OwnerName = x.Will!.OwnerAccount != null
                        ? x.Will.OwnerAccount.FullName
                        : string.Empty
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (info == null)
                return Fail(409, "This handover is not active.");

            if (!IsConfigured)
                return Fail(503, NotConfiguredMessage);

            var recipient = await _db.BeneficiaryDesignations.AsNoTracking()
                .Where(x => x.Id == designationId && x.WillId == willId)
                .Select(x => new { x.BeneficiaryAccountId })
                .FirstOrDefaultAsync(cancellationToken);

            if (recipient == null)
                return Fail(404, "Recipient not found.");

            if (recipient.BeneficiaryAccountId == null)
                return Fail(409, "This recipient has not registered yet. The code is sent once they have a verified account.");

            var delivered = await _db.HandoverItems.AnyAsync(i =>
                i.HandoverCaseId == info.Id &&
                i.Status == "DELIVERED" &&
                i.AssetAssignment!.BeneficiaryDesignationId == designationId,
                cancellationToken);

            if (!delivered)
                return Fail(409, "Deliver this recipient's assets first.");

            var row = await _db.HandoverAccessCodes.FirstOrDefaultAsync(x =>
                x.HandoverCaseId == info.Id &&
                x.BeneficiaryDesignationId == designationId,
                cancellationToken);

            if (row == null)
            {
                await IssueForCaseAsync(info.Id, cancellationToken);

                var issuedNow = await _db.HandoverAccessCodes.AsNoTracking().AnyAsync(x =>
                    x.HandoverCaseId == info.Id &&
                    x.BeneficiaryDesignationId == designationId,
                    cancellationToken);

                return issuedNow
                    ? new AccessCodeResult(true, 200, "The access code was sent to the recipient.")
                    : Fail(503, "The code could not be issued. Check the server logs and the email settings.");
            }

            if (row.VerifiedAt != null)
                return Fail(409, "The recipient already confirmed their access code.");

            var now = DateTimeOffset.UtcNow;

            if (row.LastIssuedAt.AddMinutes(ResendCooldownMinutes) > now)
            {
                return Fail(429,
                    $"A code was sent less than {ResendCooldownMinutes} minutes ago. Please wait before sending another.");
            }

            if (row.IssueCount >= MaxIssuesByExecutor)
            {
                return Fail(429,
                    "The maximum number of codes was reached for this recipient. Contact the support team.");
            }

            var code = GenerateCode();
            var salt = RandomNumberGenerator.GetBytes(16);

            row.Salt = salt;
            row.CodeHash = Hash(StripDashes(code), salt, info.Id, designationId);
            row.LastIssuedAt = now;
            row.IssueCount += 1;
            row.FailedAttempts = 0;
            row.LockedUntil = null;
            row.VerifiedAt = null;
            row.EmailedAt = null;

            _db.AuditLogs.Add(NewAudit(executorAccountId, "HANDOVER_CODE_RESENT_BY_EXECUTOR", row.Id,
                new { caseId = info.Id, issueCount = row.IssueCount }));
            await _db.SaveChangesAsync(cancellationToken);

            await SendCodeEmailAsync(
                row.Id, recipient.BeneficiaryAccountId.Value, info.OwnerName, code, cancellationToken);

            return new AccessCodeResult(true, 200,
                "A new access code was emailed to the recipient. The old one no longer works.");
        }

        // ------------------------------------------------------------------
        // Recipient: ask for help
        // ------------------------------------------------------------------
        public async Task<AccessCodeResult> RequestSupportAsync(
            Guid caseId, Guid accountId,
            CancellationToken cancellationToken = default)
        {
            var designationId = await FindDesignationIdAsync(caseId, accountId, cancellationToken);
            if (designationId == null)
                return Fail(404, "Handover not found.");

            var row = await _db.HandoverAccessCodes.AsNoTracking()
                .Where(x => x.HandoverCaseId == caseId &&
                            x.BeneficiaryDesignationId == designationId.Value)
                .Select(x => new { x.Id, x.IssueCount, x.LockedUntil, x.VerifiedAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (row == null)
                return Fail(409, "No access code was issued to you yet.");

            if (row.VerifiedAt != null)
                return Fail(409, "Your access code is already confirmed.");

            var now = DateTimeOffset.UtcNow;
            var locked = row.LockedUntil != null && row.LockedUntil.Value > now;

            if (row.IssueCount < MaxIssues && !locked)
                return Fail(409, "You can still ask for a new code yourself.");

            var alreadyAsked = await _db.AuditLogs.AsNoTracking().AnyAsync(a =>
                a.Action == "HANDOVER_CODE_SUPPORT_REQUESTED" &&
                a.EntityId == row.Id &&
                a.CreatedAt >= now.AddHours(-24),
                cancellationToken);

            if (alreadyAsked)
            {
                return Fail(429,
                    "Your request was already sent. Your Executor and the support team have been told.");
            }

            _db.AuditLogs.Add(NewAudit(accountId, "HANDOVER_CODE_SUPPORT_REQUESTED", row.Id,
                new { caseId, issueCount = row.IssueCount, locked }));
            await _db.SaveChangesAsync(cancellationToken);

            await NotifyExecutorOfSupportRequestAsync(caseId, accountId, cancellationToken);

            return new AccessCodeResult(true, 200,
                "Your request was sent. Your Executor and the support team will help you.");
        }

        private async Task NotifyExecutorOfSupportRequestAsync(
            Guid caseId, Guid accountId, CancellationToken cancellationToken)
        {
            try
            {
                var info = await _db.HandoverCases.AsNoTracking()
                    .Where(x => x.Id == caseId)
                    .Select(x => new
                    {
                        WillTitle = x.Will!.Title,
                        ExecutorEmail = x.Will.ExecutorAssignment != null &&
                                        x.Will.ExecutorAssignment.ExecutorAccount != null
                            ? x.Will.ExecutorAssignment.ExecutorAccount.Email
                            : null,
                        ExecutorName = x.Will.ExecutorAssignment != null &&
                                       x.Will.ExecutorAssignment.ExecutorAccount != null
                            ? x.Will.ExecutorAssignment.ExecutorAccount.FullName
                            : string.Empty
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                var recipientName = await _db.Accounts.AsNoTracking()
                    .Where(x => x.Id == accountId)
                    .Select(x => x.FullName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (info == null || string.IsNullOrWhiteSpace(info.ExecutorEmail)) return;

                await _email.SendHandoverSupportRequestEmailAsync(
                    info.ExecutorEmail, info.ExecutorName,
                    recipientName ?? string.Empty, info.WillTitle);
            }
            catch (Exception ex)
            {
                // The request is already in the audit trail (admins and the Executor card see it).
                _logger.LogWarning(ex, "Could not email the support request for case {CaseId}.", caseId);
            }
        }

        // ------------------------------------------------------------------
        // Executor / Owner view: where is each recipient?
        // ------------------------------------------------------------------
        public async Task<Dictionary<Guid, RecipientCodeInfo>> GetRecipientInfoAsync(
            Guid caseId, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, RecipientCodeInfo>();

            var people = await _db.HandoverItems.AsNoTracking()
                .Where(i => i.HandoverCaseId == caseId)
                .Select(i => new
                {
                    DesignationId = i.AssetAssignment!.BeneficiaryDesignationId,
                    ItemStatus = i.Status,
                    AccountId = i.AssetAssignment.BeneficiaryDesignation!.BeneficiaryAccountId,
                    Email = i.AssetAssignment.BeneficiaryDesignation.Email,
                    FullName = i.AssetAssignment.BeneficiaryDesignation.FullName
                })
                .ToListAsync(cancellationToken);

            var codes = await _db.HandoverAccessCodes.AsNoTracking()
                .Where(x => x.HandoverCaseId == caseId)
                .ToListAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;

            foreach (var group in people.GroupBy(x => x.DesignationId))
            {
                var first = group.First();
                var designationId = group.Key;
                var anyDelivered = group.Any(x => x.ItemStatus == "DELIVERED");

                if (first.AccountId == null)
                {
                    // A verified account with the same email but ANOTHER name is not linked
                    // automatically: someone has to decide (the owner or an admin).
                    var email = (first.Email ?? string.Empty).Trim().ToLower();
                    string? accountName = null;

                    if (!string.IsNullOrEmpty(email))
                    {
                        accountName = await _db.Accounts.AsNoTracking()
                            .Where(a => a.EmailVerified && a.Email.ToLower() == email)
                            .Select(a => a.FullName)
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    var nameDiffers = accountName != null &&
                        !BeneficiaryAutoLinkService.NamesMatch(accountName, first.FullName);

                    result[designationId] = new RecipientCodeInfo(
                        nameDiffers ? "NAME_MISMATCH" : "WAITING_REGISTRATION",
                        0, null, null, false);
                    continue;
                }

                if (!anyDelivered)
                {
                    result[designationId] = new RecipientCodeInfo("NOT_DELIVERED", 0, null, null, false);
                    continue;
                }

                var row = codes.FirstOrDefault(c => c.BeneficiaryDesignationId == designationId);

                if (row == null)
                {
                    result[designationId] = new RecipientCodeInfo("CODE_PENDING", 0, null, null, false);
                    continue;
                }

                var support = await HasRecentSupportRequestAsync(row.Id, row.LastIssuedAt, cancellationToken);

                string status;
                if (row.VerifiedAt != null) status = "CODE_VERIFIED";
                else if (row.LockedUntil != null && row.LockedUntil.Value > now) status = "CODE_LOCKED";
                else if (row.EmailedAt == null) status = "CODE_NOT_EMAILED";
                else status = "CODE_SENT";

                result[designationId] = new RecipientCodeInfo(
                    status, row.IssueCount, row.LockedUntil, row.LastIssuedAt, support);
            }

            return result;
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
