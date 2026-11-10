using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // C1-C5: handover minutes + signatures.
    //   start handover      -> minutes created (content + SHA-256 + PDF), one
    //                          signature row for the Executor and one for each
    //                          beneficiary
    //   Executor signs      -> only then may items be delivered      (C4)
    //   Beneficiary signs   -> after receiving (claiming) all own assets (C3)
    //   all items delivered AND all signatures SIGNED -> COMPLETED + fee released (C5)
    public partial class HandoverService
    {
        private const string MinutesPending = "PENDING_SIGNATURES";
        private const string MinutesFullySigned = "FULLY_SIGNED";
        private const string MinutesVoid = "VOID";
        private const string ExecutorRole = "EXECUTOR";
        private const string BeneficiaryRole = "BENEFICIARY";

        // ---------------------------------------------------------------
        // Create
        // ---------------------------------------------------------------
        private async Task<HandoverMinutes> EnsureMinutesAsync(Guid caseId)
        {
            var existing = await _db.HandoverMinutesDocs
                .FirstOrDefaultAsync(x => x.HandoverCaseId == caseId);

            if (existing != null) return existing;

            var c = await _db.HandoverCases
                .AsNoTracking()
                .Include(x => x.Will)
                    .ThenInclude(w => w!.OwnerAccount)
                .Include(x => x.Items)
                    .ThenInclude(i => i.AssetAssignment)
                        .ThenInclude(a => a!.WillAsset)
                            .ThenInclude(wa => wa!.Asset)
                .Include(x => x.Items)
                    .ThenInclude(i => i.AssetAssignment)
                        .ThenInclude(a => a!.BeneficiaryDesignation)
                .AsSplitQuery()
                .FirstAsync(x => x.Id == caseId);

            var executorAssignment = await _db.ExecutorAssignments
                .AsNoTracking()
                .Include(x => x.ExecutorAccount)
                .FirstOrDefaultAsync(x => x.WillId == c.WillId)
                ?? throw new InvalidOperationException(
                    "This will has no executor assignment.");

            var lines = c.Items
                .Where(i => i.AssetAssignment != null)
                .Select(i => new HandoverMinutesLine
                {
                    AssetAssignmentId = i.AssetAssignmentId,
                    AssetName = i.AssetAssignment!.WillAsset?.Asset?.Name
                        ?? string.Empty,
                    AssetType = i.AssetAssignment.WillAsset?.Asset?.AssetType
                        ?? string.Empty,
                    BeneficiaryDesignationId =
                        i.AssetAssignment.BeneficiaryDesignationId,
                    BeneficiaryName = i.AssetAssignment.BeneficiaryDesignation
                        ?.FullName ?? string.Empty,
                    Relationship = i.AssetAssignment.BeneficiaryDesignation
                        ?.Relationship,
                    AllocationPercentage =
                        i.AssetAssignment.AllocationPercentage
                })
                .OrderBy(x => x.AssetName, StringComparer.Ordinal)
                .ThenBy(x => x.BeneficiaryName, StringComparer.Ordinal)
                .ThenBy(x => x.AssetAssignmentId)
                .ToList();

            var content = new HandoverMinutesContent
            {
                Version = 1,
                HandoverCaseId = c.Id,
                WillId = c.WillId,
                WillTitle = c.Will?.Title ?? string.Empty,
                HandoverType = c.Type,
                StartedAt = HandoverMinutesBuilder
                    .TruncateToMicroseconds(c.StartedAt),
                OwnerAccountId = c.Will?.OwnerAccountId ?? Guid.Empty,
                OwnerName = c.Will?.OwnerAccount?.FullName ?? string.Empty,
                ExecutorAccountId = executorAssignment.ExecutorAccountId,
                ExecutorName = executorAssignment.ExecutorAccount?.FullName
                    ?? string.Empty,
                Items = lines
            };

            var json = HandoverMinutesBuilder.SerializeContent(content);
            var contentHash = HandoverMinutesBuilder.Sha256Hex(json);

            var now = HandoverMinutesBuilder
                .TruncateToMicroseconds(DateTimeOffset.UtcNow);

            var minutes = new HandoverMinutes
            {
                Id = Guid.NewGuid(),
                HandoverCaseId = c.Id,
                WillId = c.WillId,
                Version = 1,
                Status = MinutesPending,
                ContentJson = json,
                ContentSha256 = contentHash,
                CreatedAt = now
            };

            minutes.Signatures.Add(new HandoverSignature
            {
                Id = Guid.NewGuid(),
                MinutesId = minutes.Id,
                Role = ExecutorRole,
                SignerAccountId = executorAssignment.ExecutorAccountId,
                SignerName = content.ExecutorName,
                Status = "PENDING"
            });

            var designations = c.Items
                .Where(i => i.AssetAssignment?.BeneficiaryDesignation != null)
                .Select(i => i.AssetAssignment!.BeneficiaryDesignation!)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .OrderBy(d => d.FullName, StringComparer.Ordinal)
                .ThenBy(d => d.Id);

            foreach (var d in designations)
            {
                minutes.Signatures.Add(new HandoverSignature
                {
                    Id = Guid.NewGuid(),
                    MinutesId = minutes.Id,
                    Role = BeneficiaryRole,
                    BeneficiaryDesignationId = d.Id,
                    SignerAccountId = d.BeneficiaryAccountId,
                    SignerName = d.FullName,
                    Status = "PENDING"
                });
            }

            var pdf = HandoverMinutesBuilder.RenderPdf(
                content, contentHash, ToLines(minutes.Signatures), false);

            minutes.PdfContent = pdf;
            minutes.PdfSha256 = HandoverMinutesBuilder.Sha256Hex(pdf);

            try
            {
                _db.HandoverMinutesDocs.Add(minutes);
                await _db.SaveChangesAsync();
                return minutes;
            }
            catch (DbUpdateException)
            {
                // Two requests created the minutes at once: keep the first.
                _db.ChangeTracker.Clear();

                var winner = await _db.HandoverMinutesDocs
                    .FirstOrDefaultAsync(x => x.HandoverCaseId == caseId);

                if (winner != null) return winner;
                throw;
            }
        }

        private static List<MinutesSignatureLine> ToLines(
            IEnumerable<HandoverSignature> signatures)
        {
            return signatures
                .OrderBy(s => s.Role == ExecutorRole ? 0 : 1)
                .ThenBy(s => s.SignerName, StringComparer.Ordinal)
                .Select(s => new MinutesSignatureLine(
                    s.Role, s.SignerName, s.Status, s.SignedAt, s.SignatureHash))
                .ToList();
        }

        // ---------------------------------------------------------------
        // Checks used by the existing handover flow
        // ---------------------------------------------------------------

        // C4: no delivery before the Executor signed.
        private async Task EnsureExecutorSignedAsync(Guid caseId)
        {
            var minutes = await EnsureMinutesAsync(caseId);

            var signed = await _db.HandoverSignatures.AnyAsync(x =>
                x.MinutesId == minutes.Id &&
                x.Role == ExecutorRole &&
                x.Status == "SIGNED");

            if (!signed)
            {
                throw new WillConflictException(
                    "The Executor must sign the handover minutes before delivering assets.");
            }
        }

        // C5: complete only when everything is delivered AND everyone signed.
        private async Task<bool> TryCompleteAsync(
            Guid caseId, Guid willId, Guid executorAccountId)
        {
            var pending = await _db.HandoverItems.CountAsync(x =>
                x.HandoverCaseId == caseId && x.Status == "PENDING");

            if (pending > 0) return false;

            var minutes = await EnsureMinutesAsync(caseId);

            var unsigned = await _db.HandoverSignatures.AnyAsync(x =>
                x.MinutesId == minutes.Id && x.Status != "SIGNED");

            if (unsigned) return false;

            await CompleteAsync(caseId, willId, executorAccountId);
            return true;
        }

        // Final PDF with every signature printed. Called once, by CompleteAsync.
        private async Task SealMinutesAsync(Guid caseId)
        {
            var minutes = await _db.HandoverMinutesDocs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HandoverCaseId == caseId);

            if (minutes == null || minutes.Status != MinutesPending) return;

            var content = HandoverMinutesBuilder
                .DeserializeContent(minutes.ContentJson);

            if (content == null) return;

            var signatures = await _db.HandoverSignatures
                .AsNoTracking()
                .Where(x => x.MinutesId == minutes.Id)
                .ToListAsync();

            var pdf = HandoverMinutesBuilder.RenderPdf(
                content, minutes.ContentSha256, ToLines(signatures), true);

            var pdfHash = HandoverMinutesBuilder.Sha256Hex(pdf);
            var now = DateTimeOffset.UtcNow;

            await _db.HandoverMinutesDocs
                .Where(x => x.Id == minutes.Id && x.Status == MinutesPending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, MinutesFullySigned)
                    .SetProperty(x => x.PdfContent, pdf)
                    .SetProperty(x => x.PdfSha256, pdfHash)
                    .SetProperty(x => x.FullySignedAt, now));
        }

        // The handover was aborted: its minutes can never be signed.
        private Task VoidMinutesAsync(Guid caseId)
        {
            return _db.HandoverMinutesDocs
                .Where(x => x.HandoverCaseId == caseId &&
                            x.Status == MinutesPending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, MinutesVoid));
        }

        private async Task<HandoverMinutesSummary?> GetMinutesSummaryAsync(
            Guid caseId)
        {
            var m = await _db.HandoverMinutesDocs
                .AsNoTracking()
                .Where(x => x.HandoverCaseId == caseId)
                .Select(x => new { x.Id, x.Status })
                .FirstOrDefaultAsync();

            if (m == null) return null;

            var sigs = await _db.HandoverSignatures
                .AsNoTracking()
                .Where(x => x.MinutesId == m.Id)
                .Select(x => new { x.Role, x.Status })
                .ToListAsync();

            return new HandoverMinutesSummary
            {
                Status = m.Status,
                ExecutorSigned = sigs.Any(s =>
                    s.Role == ExecutorRole && s.Status == "SIGNED"),
                SignedCount = sigs.Count(s => s.Status == "SIGNED"),
                TotalSignatures = sigs.Count
            };
        }

        // ---------------------------------------------------------------
        // Read
        // ---------------------------------------------------------------
        private async Task<HandoverCase> GetLatestCaseAsync(Guid willId)
        {
            return await _db.HandoverCases
                .AsNoTracking()
                .Where(x => x.WillId == willId && x.Status != "ABORTED")
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Handover not found.");
        }

        // Owner / Executor / Admin: everything. A beneficiary: own lines and
        // own signature only (the hash still covers the full content).
        public async Task<HandoverMinutesResponse> GetMinutesAsync(
            Guid willId, Guid accountId, bool isAdmin)
        {
            var will = await _db.Wills
                .AsNoTracking()
                .Include(x => x.ExecutorAssignment)
                .FirstOrDefaultAsync(x => x.Id == willId)
                ?? throw new KeyNotFoundException("Handover not found.");

            var full = isAdmin ||
                will.OwnerAccountId == accountId ||
                will.ExecutorAssignment?.ExecutorAccountId == accountId;

            HashSet<Guid>? mine = null;

            if (!full)
            {
                var ids = await _db.BeneficiaryDesignations
                    .AsNoTracking()
                    .Where(x => x.WillId == willId &&
                                x.BeneficiaryAccountId == accountId)
                    .Select(x => x.Id)
                    .ToListAsync();

                if (ids.Count == 0)
                    throw new KeyNotFoundException("Handover not found.");

                mine = ids.ToHashSet();
            }

            var handoverCase = await GetLatestCaseAsync(willId);
            var minutes = await FindOrCreateForReadAsync(handoverCase);

            return await BuildMinutesResponseAsync(minutes.Id, mine);
        }

        public async Task<(byte[] Pdf, string FileName, string Sha256)>
            GetMinutesPdfAsync(Guid willId, Guid accountId, bool isAdmin)
        {
            var will = await _db.Wills
                .AsNoTracking()
                .Include(x => x.ExecutorAssignment)
                .FirstOrDefaultAsync(x => x.Id == willId)
                ?? throw new KeyNotFoundException("Handover not found.");

            var allowed = isAdmin ||
                will.OwnerAccountId == accountId ||
                will.ExecutorAssignment?.ExecutorAccountId == accountId;

            if (!allowed)
                throw new KeyNotFoundException("Handover not found.");

            var handoverCase = await GetLatestCaseAsync(willId);
            var found = await FindOrCreateForReadAsync(handoverCase);

            // Re-read without tracking: the bytes may have been replaced by SealMinutesAsync.
            var minutes = await _db.HandoverMinutesDocs
                .AsNoTracking()
                .FirstAsync(x => x.Id == found.Id);

            return (
                minutes.PdfContent,
                "handover-minutes-" + willId.ToString("N") + ".pdf",
                minutes.PdfSha256);
        }

        private async Task<HandoverMinutes> FindOrCreateForReadAsync(
            HandoverCase handoverCase)
        {
            var existing = await _db.HandoverMinutesDocs
                .FirstOrDefaultAsync(x => x.HandoverCaseId == handoverCase.Id);

            if (existing != null) return existing;

            // A handover that started before minutes existed gets them now,
            // but only while it is still running.
            if (handoverCase.Status == "IN_PROGRESS" ||
                handoverCase.Status == "PAUSED")
            {
                return await EnsureMinutesAsync(handoverCase.Id);
            }

            throw new KeyNotFoundException(
                "No minutes exist for this handover (it finished before minutes were introduced).");
        }

        private async Task<HandoverMinutesResponse> BuildMinutesResponseAsync(
            Guid minutesId, HashSet<Guid>? onlyDesignations)
        {
            var m = await _db.HandoverMinutesDocs
                .AsNoTracking()
                .FirstAsync(x => x.Id == minutesId);

            var sigs = await _db.HandoverSignatures
                .AsNoTracking()
                .Where(x => x.MinutesId == minutesId)
                .ToListAsync();

            var content = HandoverMinutesBuilder
                .DeserializeContent(m.ContentJson)
                ?? new HandoverMinutesContent();

            if (onlyDesignations != null)
            {
                content.Items = content.Items
                    .Where(i => onlyDesignations
                        .Contains(i.BeneficiaryDesignationId))
                    .ToList();

                sigs = sigs
                    .Where(s => s.Role == ExecutorRole ||
                        (s.BeneficiaryDesignationId != null &&
                         onlyDesignations.Contains(
                             s.BeneficiaryDesignationId.Value)))
                    .ToList();
            }

            return new HandoverMinutesResponse
            {
                Id = m.Id,
                HandoverCaseId = m.HandoverCaseId,
                WillId = m.WillId,
                Status = m.Status,
                ContentSha256 = m.ContentSha256,
                PdfSha256 = m.PdfSha256,
                CreatedAt = m.CreatedAt,
                FullySignedAt = m.FullySignedAt,
                Content = content,
                Signatures = sigs
                    .OrderBy(s => s.Role == ExecutorRole ? 0 : 1)
                    .ThenBy(s => s.SignerName, StringComparer.Ordinal)
                    .Select(s => new HandoverSignatureResponse
                    {
                        Id = s.Id,
                        Role = s.Role,
                        SignerName = s.SignerName,
                        BeneficiaryDesignationId = onlyDesignations == null
                            ? s.BeneficiaryDesignationId
                            : null,
                        Status = s.Status,
                        SignedAt = s.SignedAt,
                        SignatureHash = s.SignatureHash
                    })
                    .ToList()
            };
        }

        // ---------------------------------------------------------------
        // C3: sign
        // ---------------------------------------------------------------
        public async Task<HandoverMinutesResponse> SignAsExecutorAsync(
            Guid willId, Guid executorAccountId, string? ipAddress)
        {
            var isSuspended = await _db.ExecutorProfiles.AnyAsync(x =>
                x.AccountId == executorAccountId && x.IsSuspended);
            if (isSuspended)
                throw new InvalidOperationException(
                    "Suspended executors cannot sign handover minutes.");

            var isExecutor = await _db.ExecutorAssignments.AnyAsync(x =>
                x.WillId == willId &&
                x.ExecutorAccountId == executorAccountId &&
                x.Status == "ACCEPTED");

            if (!isExecutor)
                throw new KeyNotFoundException("Handover not found.");

            var handoverCase = await GetLatestCaseAsync(willId);

            if (handoverCase.Status == "PAUSED")
                throw new WillConflictException(
                    "Handover is paused while a problem is being reviewed.");

            if (handoverCase.Status != "IN_PROGRESS")
                throw new WillConflictException("Handover is not in progress.");

            var minutes = await EnsureMinutesAsync(handoverCase.Id);

            var row = await _db.HandoverSignatures
                .AsNoTracking()
                .FirstAsync(x =>
                    x.MinutesId == minutes.Id && x.Role == ExecutorRole);

            if (row.SignerAccountId != executorAccountId)
            {
                throw new WillConflictException(
                    "These minutes were prepared for a different executor. " +
                    "Ask the administrator to restart the handover.");
            }

            var now = HandoverMinutesBuilder
                .TruncateToMicroseconds(DateTimeOffset.UtcNow);

            var hash = HandoverMinutesBuilder.SignatureHash(
                minutes.ContentSha256, ExecutorRole,
                executorAccountId, null, now);

            var contentHash = minutes.ContentSha256;

            // Atomic: a signature is given once.
            var rows = await _db.HandoverSignatures
                .Where(x => x.Id == row.Id && x.Status == "PENDING")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "SIGNED")
                    .SetProperty(x => x.SignedAt, now)
                    .SetProperty(x => x.SignedByAccountId, executorAccountId)
                    .SetProperty(x => x.SignedContentSha256, contentHash)
                    .SetProperty(x => x.SignatureHash, hash)
                    .SetProperty(x => x.IpAddress, ipAddress));

            if (rows == 0)
                throw new WillConflictException(
                    "You already signed these minutes.");

            AddAudit(executorAccountId, "HANDOVER_MINUTES_SIGNED_EXECUTOR",
                "HandoverMinutes", minutes.Id,
                new { willId, handoverCaseId = handoverCase.Id, contentSha256 = contentHash });
            await _db.SaveChangesAsync();

            return await BuildMinutesResponseAsync(minutes.Id, null);
        }

        // The beneficiary confirms they received their assets. Every asset of
        // theirs in this handover must already be received (face scan).
        public async Task<HandoverMinutesResponse> SignAsBeneficiaryAsync(
            Guid willId, Guid beneficiaryAccountId, string? ipAddress)
        {
            var handoverCase = await GetLatestCaseAsync(willId);

            var myItems = await _db.HandoverItems
                .AsNoTracking()
                .Where(i =>
                    i.HandoverCaseId == handoverCase.Id &&
                    i.AssetAssignment!.BeneficiaryDesignation!
                        .BeneficiaryAccountId == beneficiaryAccountId)
                .Select(i => new
                {
                    i.Status,
                    i.ClaimedAt,
                    DesignationId = i.AssetAssignment!.BeneficiaryDesignationId
                })
                .ToListAsync();

            if (myItems.Count == 0)
                throw new KeyNotFoundException("Handover not found.");

            if (handoverCase.Status == "PAUSED")
                throw new WillConflictException(
                    "This handover is paused while a problem is being reviewed.");

            if (handoverCase.Status != "IN_PROGRESS")
                throw new WillConflictException(
                    "These minutes can no longer be signed.");

            if (myItems.Any(i => i.Status != "DELIVERED" || i.ClaimedAt == null))
            {
                throw new WillConflictException(
                    "Receive all your assets (access code and face scan) before signing the minutes.");
            }

            var minutes = await EnsureMinutesAsync(handoverCase.Id);
            var contentHash = minutes.ContentSha256;
            var designationIds = myItems
                .Select(i => i.DesignationId)
                .Distinct()
                .ToList();

            var now = HandoverMinutesBuilder
                .TruncateToMicroseconds(DateTimeOffset.UtcNow);

            var signedAny = false;

            foreach (var designationId in designationIds)
            {
                var hash = HandoverMinutesBuilder.SignatureHash(
                    contentHash, BeneficiaryRole,
                    beneficiaryAccountId, designationId, now);

                var rows = await _db.HandoverSignatures
                    .Where(x => x.MinutesId == minutes.Id &&
                                x.Role == BeneficiaryRole &&
                                x.BeneficiaryDesignationId == designationId &&
                                x.Status == "PENDING")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, "SIGNED")
                        .SetProperty(x => x.SignerAccountId, beneficiaryAccountId)
                        .SetProperty(x => x.SignedAt, now)
                        .SetProperty(x => x.SignedByAccountId, beneficiaryAccountId)
                        .SetProperty(x => x.SignedContentSha256, contentHash)
                        .SetProperty(x => x.SignatureHash, hash)
                        .SetProperty(x => x.IpAddress, ipAddress));

                if (rows > 0) signedAny = true;
            }

            if (!signedAny)
                throw new WillConflictException(
                    "You already signed these minutes.");

            AddAudit(beneficiaryAccountId, "HANDOVER_MINUTES_SIGNED_BENEFICIARY",
                "HandoverMinutes", minutes.Id,
                new { willId, handoverCaseId = handoverCase.Id, contentSha256 = contentHash });
            await _db.SaveChangesAsync();

            // The last signature may be the one that completes the handover.
            var executorId = await _db.ExecutorAssignments
                .Where(x => x.WillId == willId)
                .Select(x => x.ExecutorAccountId)
                .FirstAsync();

            await TryCompleteAsync(handoverCase.Id, willId, executorId);

            return await BuildMinutesResponseAsync(
                minutes.Id, designationIds.ToHashSet());
        }
    }
}
