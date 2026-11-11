using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    public class LegalService : ILegalService
    {
        private const long MaxBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> Allowed =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = "application/pdf",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png"
            };

        private readonly LegacyVaultDbContext _db;
        private readonly ILifeStatusClient _lifeStatus;
        private readonly IConfiguration _config;
        private readonly IEmailSender _email;
        private readonly IAccountLockService _lock;
        private readonly ILogger<LegalService> _logger;

        public LegalService(
            LegacyVaultDbContext db,
            ILifeStatusClient lifeStatus,
            IConfiguration config,
            IEmailSender email,
            IAccountLockService accountLock,
            ILogger<LegalService> logger)
        {
            _db = db;
            _lifeStatus = lifeStatus;
            _config = config;
            _email = email;
            _lock = accountLock;
            _logger = logger;
        }

        private string StorageRoot =>
            Path.GetFullPath(
                _config["Storage:Root"]
                ?? Path.Combine(AppContext.BaseDirectory, "App_Data"));

        public async Task<LegalCaseSummaryResponse> SubmitAsync(
            Guid executorAccountId,
            Guid willId,
            IFormFile file,
            string? certificateData = null)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is required.");

            if (file.Length > MaxBytes)
                throw new InvalidOperationException("File must be 5 MB or smaller.");

            var ext = Path.GetExtension(file.FileName);
            if (!Allowed.TryGetValue(ext, out var contentType))
                throw new InvalidOperationException(
                    "Only PDF, JPG or PNG files are accepted.");

            // Only the ACCEPTED executor of this will, and only after the
            // DMS trigger (threshold reached).
            var will = await _db.Wills
                .Include(x => x.ExecutorAssignment)
                .Include(x => x.DmsConfiguration)
                .Include(x => x.OwnerAccount)
                .FirstOrDefaultAsync(x => x.Id == willId)
                ?? throw new KeyNotFoundException("Will not found.");

            var isSuspended = await _db.ExecutorProfiles.AnyAsync(x =>
                x.AccountId == executorAccountId && x.IsSuspended);
            if (isSuspended)
                throw new InvalidOperationException("Suspended executors cannot submit legal cases.");

            var assignment = will.ExecutorAssignment;
            if (assignment == null ||
                assignment.ExecutorAccountId != executorAccountId ||
                assignment.Status != "ACCEPTED")
            {
                throw new KeyNotFoundException("Will not found.");
            }

            if (will.Status != "ACTIVE" ||
                will.DmsConfiguration?.Status != "TRIGGERED")
            {
                throw new InvalidOperationException(
                    "A death certificate can be submitted only after the DMS trigger.");
            }

            var open = await _db.LegalCases.AnyAsync(x =>
                x.WillId == willId &&
                (x.Status == "PENDING" || x.Status == "APPROVED"));

            if (open)
            {
                throw new WillConflictException(
                    "This will already has a pending or approved legal case.");
            }

            // Optional form data typed by the Executor. Only keep valid JSON,
            // capped in size, so it can be shown safely to Legal Verify.
            string? cleanData = null;
            if (!string.IsNullOrWhiteSpace(certificateData))
            {
                if (certificateData.Length > 4000)
                    throw new InvalidOperationException("Certificate data is too long.");
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(certificateData);
                    if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                        throw new InvalidOperationException("Certificate data must be a JSON object.");
                    cleanData = certificateData;
                }
                catch (System.Text.Json.JsonException)
                {
                    throw new InvalidOperationException("Certificate data is not valid JSON.");
                }
            }

            // C8: the content must really be a PDF / JPG / PNG, is hashed, and is
            // stored ENCRYPTED. We never fall back to storing plain text.
            byte[] plain;
            await using (var input = file.OpenReadStream())
            using (var buffer = new MemoryStream())
            {
                await input.CopyToAsync(buffer);
                plain = buffer.ToArray();
            }

            if (!CertificateCrypto.MatchesExtension(ext, plain))
            {
                throw new InvalidOperationException(
                    "The file content does not match a PDF, JPG or PNG.");
            }

            var caseId = Guid.NewGuid();
            var fileSha256 = HandoverMinutesBuilder.Sha256Hex(plain);
            var encrypted = CertificateCrypto.Encrypt(GetKey(), caseId, plain);

            // Random name, no original name or extension on disk.
            var relative = Path.Combine(
                "death-certificates", caseId.ToString("N") + ".enc");
            var full = Path.Combine(StorageRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllBytesAsync(full, encrypted);

            var legalCase = new LegalCase
            {
                Id = caseId,
                WillId = willId,
                ExecutorAccountId = executorAccountId,
                FilePath = relative,
                // Never trust the client path; keep only the file name.
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = contentType,
                FileSha256 = fileSha256,
                IsEncrypted = true,
                CertificateData = cleanData,
                Status = "PENDING",
                SubmittedAt = DateTimeOffset.UtcNow
            };

            _db.LegalCases.Add(legalCase);
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = executorAccountId, Action = "LEGAL_CASE_SUBMITTED",
                EntityType = "LegalCase", EntityId = legalCase.Id,
                Details = System.Text.Json.JsonSerializer.Serialize(new { willId, legalCase.ContentType, fileLength = file.Length }),
                CreatedAt = DateTimeOffset.UtcNow
            });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                // No database row -> do not leave an orphan file behind.
                try { File.Delete(full); } catch { /* best effort */ }
                throw;
            }

            // C8: tell the Owner (they may be alive) and the Legal Verifiers.
            await NotifySubmittedAsync(will);

            return new LegalCaseSummaryResponse
            {
                Id = legalCase.Id,
                WillId = willId,
                WillTitle = will.Title,
                OwnerName = will.OwnerAccount?.FullName ?? string.Empty,
                ExecutorName = await _db.Accounts
                    .Where(a => a.Id == executorAccountId)
                    .Select(a => a.FullName).FirstAsync(),
                Status = legalCase.Status,
                SubmittedAt = legalCase.SubmittedAt
            };
        }

        public async Task<List<LegalCaseSummaryResponse>> ListAsync(
            Guid accountId,
            bool isLegalVerifier,
            string? status)
        {
            var query = _db.LegalCases.AsNoTracking().AsQueryable();

            if (!isLegalVerifier)
                query = query.Where(x => x.ExecutorAccountId == accountId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                query = query.Where(x => x.Status == s);
            }

            return await query
                .OrderByDescending(x => x.SubmittedAt)
                .Select(x => new LegalCaseSummaryResponse
                {
                    Id = x.Id,
                    WillId = x.WillId,
                    WillTitle = x.Will!.Title,
                    OwnerName = x.Will.OwnerAccount != null
                        ? x.Will.OwnerAccount.FullName : string.Empty,
                    ExecutorName = x.ExecutorAccount != null
                        ? x.ExecutorAccount.FullName : string.Empty,
                    Status = x.Status,
                    SubmittedAt = x.SubmittedAt,
                    ReviewedAt = x.ReviewedAt,
                    ReviewNote = x.ReviewNote
                })
                .ToListAsync();
        }

        public async Task<LegalCaseDetailResponse> GetAsync(
            Guid caseId,
            Guid accountId,
            bool isLegalVerifier)
        {
            var c = await LoadForAccessAsync(caseId, accountId, isLegalVerifier);

            var dms = await _db.DmsConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WillId == c.WillId);

            // Read-only value from the third-party app (R10).
            var life = await _lifeStatus.GetStatusAsync(
                c.Will!.OwnerAccountId, CancellationToken.None);

            var assetCount = await _db.WillAssets
                .CountAsync(x => x.WillId == c.WillId);
            var beneficiaryCount = await _db.BeneficiaryDesignations
                .CountAsync(x => x.WillId == c.WillId);

            return new LegalCaseDetailResponse
            {
                Id = c.Id,
                WillId = c.WillId,
                WillTitle = c.Will.Title,
                OwnerName = c.Will.OwnerAccount?.FullName ?? string.Empty,
                ExecutorName = c.ExecutorAccount?.FullName ?? string.Empty,
                Status = c.Status,
                SubmittedAt = c.SubmittedAt,
                ReviewedAt = c.ReviewedAt,
                OriginalFileName = c.OriginalFileName,
                ReviewNote = c.ReviewNote,
                CertificateData = c.CertificateData,
                OwnerEmail = c.Will.OwnerAccount?.Email,
                AssetCount = assetCount,
                BeneficiaryCount = beneficiaryCount,
                LifeStatus = life,
                MissedCheckIns = dms?.MissedCheckIns ?? 0,
                MaxMissedCheckIns = dms?.MaxMissedCheckIns ?? 0,
                DmsThresholdReachedAt = dms?.ThresholdReachedAt
            };
        }

        public async Task<(byte[] Content, string ContentType, string FileName)>
            GetCertificateAsync(
                Guid caseId,
                Guid accountId,
                bool isLegalVerifier)
        {
            var c = await LoadForAccessAsync(caseId, accountId, isLegalVerifier);

            var full = Path.GetFullPath(Path.Combine(StorageRoot, c.FilePath));

            // Defence in depth: never serve outside the storage root.
            if (!full.StartsWith(StorageRoot, StringComparison.Ordinal) ||
                !File.Exists(full))
            {
                throw new KeyNotFoundException("Certificate file not found.");
            }

            var stored = await File.ReadAllBytesAsync(full);

            // Old files (before C8) are plain; new ones are encrypted.
            var content = c.IsEncrypted
                ? CertificateCrypto.Decrypt(GetKey(), c.Id, stored)
                : stored;

            if (!string.IsNullOrEmpty(c.FileSha256) &&
                !string.Equals(
                    HandoverMinutesBuilder.Sha256Hex(content),
                    c.FileSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The stored certificate failed its integrity check.");
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = accountId,
                Action = "LEGAL_CERTIFICATE_VIEWED",
                EntityType = "LegalCase",
                EntityId = caseId,
                Details = System.Text.Json.JsonSerializer.Serialize(
                    new { c.WillId, isLegalVerifier }),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync();

            return (content, c.ContentType, c.OriginalFileName);
        }

        public async Task<LegalCaseDetailResponse> WithdrawAsync(
            Guid caseId,
            Guid executorAccountId)
        {
            var exists = await _db.LegalCases.AnyAsync(x =>
                x.Id == caseId && x.ExecutorAccountId == executorAccountId);

            if (!exists)
                throw new KeyNotFoundException("Legal case not found.");

            var now = DateTimeOffset.UtcNow;

            // Atomic: only a PENDING case can be withdrawn.
            var rows = await _db.LegalCases
                .Where(x => x.Id == caseId && x.Status == "PENDING")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "CANCELLED")
                    .SetProperty(x => x.ReviewNote, "Withdrawn by the Executor.")
                    .SetProperty(x => x.ReviewedAt, now));

            if (rows == 0)
                throw new WillConflictException(
                    "Only a pending legal case can be withdrawn.");

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = executorAccountId,
                Action = "LEGAL_CASE_WITHDRAWN",
                EntityType = "LegalCase",
                EntityId = caseId,
                Details = "{}",
                CreatedAt = now
            });
            await _db.SaveChangesAsync();

            return await GetAsync(caseId, executorAccountId, false);
        }

        public async Task<LegalCaseDetailResponse> DecideAsync(
            Guid caseId,
            Guid reviewerAccountId,
            LegalDecisionRequest request)
        {
            if (!request.Approve &&
                string.IsNullOrWhiteSpace(request.Note))
            {
                throw new InvalidOperationException(
                    "A note is required when rejecting.");
            }

            var exists = await _db.LegalCases.AnyAsync(x => x.Id == caseId);
            if (!exists) throw new KeyNotFoundException("Legal case not found.");

            var now = DateTimeOffset.UtcNow;
            var status = request.Approve ? "APPROVED" : "REJECTED";
            var note = string.IsNullOrWhiteSpace(request.Note)
                ? null : request.Note.Trim();

            // Atomic: two reviewers cannot both decide the same case.
            var rows = await _db.LegalCases
                .Where(x => x.Id == caseId && x.Status == "PENDING")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, status)
                    .SetProperty(x => x.ReviewedByAccountId, reviewerAccountId)
                    .SetProperty(x => x.ReviewNote, note)
                    .SetProperty(x => x.ReviewedAt, now));

            if (rows == 0)
                throw new WillConflictException("This case was already decided.");

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = reviewerAccountId,
                Action = request.Approve ? "LEGAL_CASE_APPROVED" : "LEGAL_CASE_REJECTED",
                EntityType = "LegalCase", EntityId = caseId,
                Details = System.Text.Json.JsonSerializer.Serialize(new { note }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync();

            // C8 + N7: lock the Owner on approval and e-mail the Executor.
            await AfterDecisionAsync(caseId, request.Approve, note, reviewerAccountId);

            // Approval alone does NOT hand anything over (R05): the
            // Executor must still start handover, which re-checks LifeStatus.
            return await GetAsync(caseId, reviewerAccountId, true);
        }

        private byte[] GetKey()
        {
            var kek = _config["Encryption:KekBase64"];

            if (string.IsNullOrWhiteSpace(kek))
            {
                throw new InvalidOperationException(
                    "Certificate storage is not configured (Encryption:KekBase64 is missing).");
            }

            return CertificateCrypto.DeriveKey(kek);
        }

        private async Task TrySendAsync(Func<Task> send, string what)
        {
            try { await send(); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "{What} e-mail failed.", what);
            }
        }

        // The Owner may be alive, so they are told first; Legal gets the review request.
        private async Task NotifySubmittedAsync(Will will)
        {
            var ownerName = will.OwnerAccount?.FullName ?? string.Empty;

            if (will.OwnerAccount != null)
            {
                var owner = will.OwnerAccount;
                await TrySendAsync(() =>
                    _email.SendDeathCertificateSubmittedEmailAsync(
                        owner.Email, owner.FullName, ownerName, will.Title, true),
                    "Death certificate (owner)");
            }

            var verifiers = await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Role == "LEGAL_VERIFIER" && a.EmailVerified)
                .Select(a => new { a.Email, a.FullName })
                .ToListAsync();

            foreach (var v in verifiers)
            {
                await TrySendAsync(() =>
                    _email.SendDeathCertificateSubmittedEmailAsync(
                        v.Email, v.FullName, ownerName, will.Title, false),
                    "Death certificate (legal)");
            }
        }

        private async Task AfterDecisionAsync(
            Guid caseId, bool approved, string? note, Guid reviewerAccountId)
        {
            var c = await _db.LegalCases
                .AsNoTracking()
                .Include(x => x.Will)
                    .ThenInclude(w => w!.OwnerAccount)
                .Include(x => x.ExecutorAccount)
                .FirstOrDefaultAsync(x => x.Id == caseId);

            if (c?.Will == null) return;

            if (approved)
            {
                try
                {
                    await _lock.LockOwnerAsync(
                        c.Will.OwnerAccountId, c.Id,
                        "Death certificate approved", CancellationToken.None);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The approval stands; the failure is visible so it can be retried.
                    _logger.LogError(ex, "Owner lock failed for case {CaseId}.", c.Id);

                    _db.AuditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        ActorAccountId = reviewerAccountId,
                        Action = "OWNER_LOCK_FAILED",
                        EntityType = "LegalCase",
                        EntityId = c.Id,
                        Details = System.Text.Json.JsonSerializer.Serialize(
                            new { c.Will.OwnerAccountId }),
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }
            }

            var executor = c.ExecutorAccount;
            if (executor != null)
            {
                await TrySendAsync(() =>
                    _email.SendLegalDecisionEmailAsync(
                        executor.Email,
                        executor.FullName,
                        c.Will.OwnerAccount?.FullName ?? string.Empty,
                        c.Will.Title,
                        approved,
                        note),
                    "Legal decision");
            }
        }

        private async Task<LegalCase> LoadForAccessAsync(
            Guid caseId,
            Guid accountId,
            bool isLegalVerifier)
        {
            var c = await _db.LegalCases
                .AsNoTracking()
                .Include(x => x.Will)
                    .ThenInclude(w => w!.OwnerAccount)
                .Include(x => x.ExecutorAccount)
                .FirstOrDefaultAsync(x => x.Id == caseId);

            if (c == null ||
                (!isLegalVerifier && c.ExecutorAccountId != accountId))
            {
                throw new KeyNotFoundException("Legal case not found.");
            }

            return c;
        }
    }
}
