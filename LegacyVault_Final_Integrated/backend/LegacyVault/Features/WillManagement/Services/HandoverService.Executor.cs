using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // C9: Executor-side handover APIs.
    public partial class HandoverService
    {
        // The Executor cancels a DEATH handover that has not delivered anything
        // yet (for example it was started by mistake). The will goes back to
        // ACTIVE, the minutes are voided, the escrowed fee stays held, and the
        // Executor may start the handover again later.
        // Once an asset was delivered, only an Admin abort (with refund) applies.
        public async Task<HandoverResponse> CancelDeathHandoverAsync(
            Guid willId, Guid executorAccountId, string reason)
        {
            var isExecutor = await _db.ExecutorAssignments.AnyAsync(x =>
                x.WillId == willId &&
                x.ExecutorAccountId == executorAccountId &&
                x.Status == "ACCEPTED");

            if (!isExecutor)
                throw new KeyNotFoundException("Handover not found.");

            var handoverCase = await _db.HandoverCases
                .AsNoTracking()
                .Where(x => x.WillId == willId &&
                            (x.Status == "IN_PROGRESS" || x.Status == "PAUSED"))
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("No handover in progress.");

            if (handoverCase.Type != "DEATH")
            {
                throw new InvalidOperationException(
                    "Only a death handover can be cancelled by the Executor.");
            }

            var touched = await _db.HandoverItems.AnyAsync(x =>
                x.HandoverCaseId == handoverCase.Id &&
                (x.Status == "DELIVERED" || x.ClaimedAt != null));

            if (touched)
            {
                throw new WillConflictException(
                    "Some assets were already delivered. Ask the administrator to abort the handover.");
            }

            var now = DateTimeOffset.UtcNow;
            var caseId = handoverCase.Id;

            var rows = await _db.HandoverCases
                .Where(x => x.Id == caseId &&
                            (x.Status == "IN_PROGRESS" || x.Status == "PAUSED"))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "ABORTED")
                    .SetProperty(x => x.CompletedAt, (DateTimeOffset?)now));

            if (rows == 0)
                throw new WillConflictException("Handover changed; refresh and retry.");

            await VoidMinutesAsync(caseId);

            await _db.Wills
                .Where(x => x.Id == willId && x.Status == "HANDOVER_IN_PROGRESS")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "ACTIVE")
                    .SetProperty(x => x.UpdatedAt, now));

            AddAudit(executorAccountId, "HANDOVER_CANCELLED_BY_EXECUTOR",
                "HandoverCase", caseId, new { willId, reason });
            await _db.SaveChangesAsync();

            return await BuildResponseAsync(caseId);
        }

        // Everything the Executor needs on one screen, with the next step.
        public async Task<List<ExecutorHandoverSummaryResponse>>
            GetExecutorHandoversAsync(Guid executorAccountId)
        {
            var wills = await _db.ExecutorAssignments
                .AsNoTracking()
                .Where(x => x.ExecutorAccountId == executorAccountId &&
                            x.Status == "ACCEPTED" &&
                            x.Will != null)
                .Select(x => new
                {
                    x.WillId,
                    WillTitle = x.Will!.Title,
                    OwnerName = x.Will.OwnerAccount != null
                        ? x.Will.OwnerAccount.FullName : string.Empty,
                    WillStatus = x.Will.Status,
                    DmsStatus = x.Will.DmsConfiguration != null
                        ? x.Will.DmsConfiguration.Status : null
                })
                .ToListAsync();

            var willIds = wills.Select(x => x.WillId).ToList();

            var legalRows = await _db.LegalCases
                .AsNoTracking()
                .Where(x => willIds.Contains(x.WillId))
                .Select(x => new { x.WillId, x.Status, x.SubmittedAt })
                .ToListAsync();

            var caseRows = await _db.HandoverCases
                .AsNoTracking()
                .Where(x => willIds.Contains(x.WillId) && x.Status != "ABORTED")
                .Select(x => new
                {
                    x.Id, x.WillId, x.Status, x.Type, x.StartedAt,
                    Total = x.Items.Count,
                    Delivered = x.Items.Count(i => i.Status == "DELIVERED")
                })
                .ToListAsync();

            var result = new List<ExecutorHandoverSummaryResponse>();

            foreach (var w in wills)
            {
                var legal = legalRows
                    .Where(x => x.WillId == w.WillId)
                    .OrderByDescending(x => x.SubmittedAt)
                    .FirstOrDefault();

                var hc = caseRows
                    .Where(x => x.WillId == w.WillId)
                    .OrderByDescending(x => x.StartedAt)
                    .FirstOrDefault();

                // Only wills that matter for a handover.
                if (w.DmsStatus != "TRIGGERED" && legal == null && hc == null)
                    continue;

                HandoverMinutesSummary? minutes = null;
                if (hc != null)
                    minutes = await GetMinutesSummaryAsync(hc.Id);

                string next;
                if (hc == null)
                {
                    next = legal == null || legal.Status == "REJECTED" ||
                           legal.Status == "CANCELLED"
                        ? "SUBMIT_DEATH_CERTIFICATE"
                        : legal.Status == "PENDING"
                            ? "WAIT_LEGAL_REVIEW"
                            : "START_HANDOVER";
                }
                else if (hc.Status == "COMPLETED")
                {
                    next = "DONE";
                }
                else if (hc.Status == "PAUSED")
                {
                    next = "PAUSED";
                }
                else if (minutes != null && !minutes.ExecutorSigned)
                {
                    next = "SIGN_MINUTES";
                }
                else if (hc.Delivered < hc.Total)
                {
                    next = "DELIVER_ASSETS";
                }
                else
                {
                    next = "WAIT_BENEFICIARY_SIGNATURES";
                }

                result.Add(new ExecutorHandoverSummaryResponse
                {
                    WillId = w.WillId,
                    WillTitle = w.WillTitle,
                    OwnerName = w.OwnerName,
                    WillStatus = w.WillStatus,
                    DmsStatus = w.DmsStatus,
                    LegalCaseStatus = legal?.Status,
                    HandoverStatus = hc?.Status,
                    HandoverType = hc?.Type,
                    TotalItems = hc?.Total ?? 0,
                    DeliveredItems = hc?.Delivered ?? 0,
                    Minutes = minutes,
                    NextStep = next
                });
            }

            return result;
        }
    }
}
