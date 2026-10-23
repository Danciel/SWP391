using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // Handover state machine (kept separate from DMS):
    //   Will ACTIVE --start--> HANDOVER_IN_PROGRESS --all items delivered--> HANDED_OVER
    //   HANDOVER_IN_PROGRESS --admin abort--> ACTIVE (fee refunded)
    public class HandoverService : IHandoverService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly IWalletService _wallet;
        private readonly ILifeStatusClient _lifeStatus;
        private readonly IEmailSender _email;
        private readonly ILogger<HandoverService> _logger;

        public HandoverService(
            LegacyVaultDbContext db,
            IWalletService wallet,
            ILifeStatusClient lifeStatus,
            IEmailSender email,
            ILogger<HandoverService> logger)
        {
            _db = db;
            _wallet = wallet;
            _lifeStatus = lifeStatus;
            _email = email;
            _logger = logger;
        }

        public async Task<HandoverResponse> StartImmediateAsync(
            Guid willId, Guid ownerAccountId)
        {
            var will = await _db.Wills
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == willId && x.OwnerAccountId == ownerAccountId)
                ?? throw new KeyNotFoundException("Will not found.");

            // Alive owner chooses this on purpose: no DMS, no certificate.
            return await StartAsync(will.Id, "IMMEDIATE", ownerAccountId);
        }

        public async Task<HandoverResponse> StartAfterDeathAsync(
            Guid willId, Guid executorAccountId)
        {
            var assignment = await _db.ExecutorAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.WillId == willId &&
                    x.ExecutorAccountId == executorAccountId &&
                    x.Status == "ACCEPTED")
                ?? throw new KeyNotFoundException("Will not found.");

            var isSuspended = await _db.ExecutorProfiles.AnyAsync(x =>
                x.AccountId == executorAccountId && x.IsSuspended);
            if (isSuspended)
                throw new InvalidOperationException("Suspended executors cannot start a handover.");

            var will = await _db.Wills.AsNoTracking()
                .FirstAsync(x => x.Id == willId);

            var approved = await _db.LegalCases.AnyAsync(x =>
                x.WillId == willId && x.Status == "APPROVED");

            if (!approved)
            {
                throw new InvalidOperationException(
                    "Legal Verify must approve a death certificate first.");
            }

            // Source of truth is the third-party app (read-only).
            var life = await _lifeStatus.GetStatusAsync(
                will.OwnerAccountId, CancellationToken.None);

            if (life != "DECEASED")
            {
                throw new WillConflictException(
                    $"Handover cannot start: LifeStatus is {life}.");
            }

            return await StartAsync(willId, "DEATH", assignment.ExecutorAccountId);
        }

        private async Task<HandoverResponse> StartAsync(
            Guid willId, string type, Guid initiatedBy)
        {
            var hasAssignments = await _db.AssetAssignments.AnyAsync(x =>
                x.WillAsset!.WillId == willId);

            if (!hasAssignments)
            {
                throw new InvalidOperationException(
                    "This will has no asset assignments to hand over.");
            }

            // H06: nothing starts while the fee is under dispute.
            if (await _db.PaymentDisputes.AnyAsync(x =>
                    x.WillId == willId && x.Status == "OPEN"))
            {
                throw new WillConflictException(
                    "A payment dispute is open for this will. Resolve it before starting a handover.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            // Atomic: only one request can move ACTIVE -> HANDOVER_IN_PROGRESS.
            var moved = await _db.Wills
                .Where(x => x.Id == willId && x.Status == "ACTIVE")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "HANDOVER_IN_PROGRESS")
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));

            if (moved == 0)
            {
                throw new WillConflictException(
                    "Handover can start only for an ACTIVE will.");
            }

            var now = DateTimeOffset.UtcNow;
            var handover = new HandoverCase
            {
                Id = Guid.NewGuid(),
                WillId = willId,
                Type = type,
                Status = "IN_PROGRESS",
                InitiatedByAccountId = initiatedBy,
                StartedAt = now
            };

            var assignmentIds = await _db.AssetAssignments
                .Where(x => x.WillAsset!.WillId == willId)
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var id in assignmentIds)
            {
                handover.Items.Add(new HandoverItem
                {
                    Id = Guid.NewGuid(),
                    HandoverCaseId = handover.Id,
                    AssetAssignmentId = id,
                    Status = "PENDING"
                });
            }

            _db.HandoverCases.Add(handover);
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = initiatedBy, Action = "HANDOVER_STARTED",
                EntityType = "HandoverCase", EntityId = handover.Id,
                Details = System.Text.Json.JsonSerializer.Serialize(new { willId, type, itemCount = handover.Items.Count }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await InviteUnregisteredAsync(willId);

            return await BuildResponseAsync(handover.Id);
        }

        // LV-16: people without an account get an invitation email.
        private async Task InviteUnregisteredAsync(Guid willId)
        {
            var ownerName = await _db.Wills
                .Where(x => x.Id == willId)
                .Select(x => x.OwnerAccount!.FullName)
                .FirstAsync();

            var people = await _db.BeneficiaryDesignations
                .AsNoTracking()
                .Where(x => x.WillId == willId &&
                            x.BeneficiaryAccountId == null)
                .ToListAsync();

            foreach (var p in people)
            {
                try
                {
                    await _email.SendBeneficiaryInviteEmailAsync(
                        p.Email, p.FullName, ownerName);
                }
                catch (Exception ex)
                {
                    // Invitation failure must not undo the handover.
                    _logger.LogWarning(ex,
                        "Invite email failed for beneficiary {Id}.", p.Id);
                }
            }
        }

        public async Task<HandoverResponse> GetAsync(
            Guid willId, Guid accountId)
        {
            var will = await _db.Wills.AsNoTracking()
                .Include(x => x.ExecutorAssignment)
                .FirstOrDefaultAsync(x => x.Id == willId)
                ?? throw new KeyNotFoundException("Handover not found.");

            var isOwner = will.OwnerAccountId == accountId;
            var isExecutor =
                will.ExecutorAssignment?.ExecutorAccountId == accountId;

            var caseId = await _db.HandoverCases
                .Where(x => x.WillId == willId)
                .OrderByDescending(x => x.StartedAt)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Handover not found.");

            // A beneficiary sees only the items that are theirs.
            Guid? onlyAccount = null;
            if (!isOwner && !isExecutor)
            {
                var linked = await _db.BeneficiaryDesignations.AnyAsync(x =>
                    x.WillId == willId &&
                    x.BeneficiaryAccountId == accountId);

                if (!linked)
                    throw new KeyNotFoundException("Handover not found.");

                onlyAccount = accountId;
            }

            return await BuildResponseAsync(caseId, onlyAccount);
        }

        public async Task<HandoverResponse> DeliverAsync(
            Guid itemId, Guid executorAccountId)
        {
            var isSuspended = await _db.ExecutorProfiles.AnyAsync(x =>
                x.AccountId == executorAccountId && x.IsSuspended);
            if (isSuspended)
                throw new InvalidOperationException("Suspended executors cannot deliver handover items.");

            var item = await _db.HandoverItems
                .AsNoTracking()
                .Include(x => x.HandoverCase)
                .FirstOrDefaultAsync(x => x.Id == itemId)
                ?? throw new KeyNotFoundException("Handover item not found.");

            var willId = item.HandoverCase!.WillId;

            var isExecutor = await _db.ExecutorAssignments.AnyAsync(x =>
                x.WillId == willId &&
                x.ExecutorAccountId == executorAccountId &&
                x.Status == "ACCEPTED");

            if (!isExecutor)
                throw new KeyNotFoundException("Handover item not found.");

            if (item.HandoverCase.Status == "PAUSED")
                throw new WillConflictException(
                    "Handover is paused while a problem is being reviewed.");

            if (item.HandoverCase.Status != "IN_PROGRESS")
                throw new WillConflictException("Handover is not in progress.");

            var now = DateTimeOffset.UtcNow;

            // Atomic: an item can be delivered once.
            var rows = await _db.HandoverItems
                .Where(x => x.Id == itemId && x.Status == "PENDING")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "DELIVERED")
                    .SetProperty(x => x.DeliveredAt, now)
                    .SetProperty(x => x.DeliveredByAccountId, executorAccountId));

            if (rows == 0)
                throw new WillConflictException("Item was already delivered.");

            var pending = await _db.HandoverItems.CountAsync(x =>
                x.HandoverCaseId == item.HandoverCaseId &&
                x.Status == "PENDING");

            if (pending == 0)
                await CompleteAsync(item.HandoverCaseId, willId, executorAccountId);

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = executorAccountId, Action = "HANDOVER_ITEM_DELIVERED",
                EntityType = "HandoverItem", EntityId = itemId,
                Details = System.Text.Json.JsonSerializer.Serialize(new { willId, handoverCaseId = item.HandoverCaseId, remainingItems = pending }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync();
            return await BuildResponseAsync(item.HandoverCaseId);
        }

        private async Task CompleteAsync(
            Guid caseId, Guid willId, Guid executorAccountId)
        {
            var now = DateTimeOffset.UtcNow;

            // Only the caller that flips IN_PROGRESS -> COMPLETED continues,
            // so the fee is released exactly once.
            var done = await _db.HandoverCases
                .Where(x => x.Id == caseId && x.Status == "IN_PROGRESS")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "COMPLETED")
                    .SetProperty(x => x.CompletedAt, now));

            if (done == 0) return;

            await _db.Wills
                .Where(x => x.Id == willId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "HANDED_OVER")
                    .SetProperty(x => x.UpdatedAt, now));

            await _db.ExecutorProfiles
                .Where(x => x.AccountId == executorAccountId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CompletedCases, x => x.CompletedCases + 1));

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = executorAccountId, Action = "HANDOVER_COMPLETED",
                EntityType = "HandoverCase", EntityId = caseId,
                Details = System.Text.Json.JsonSerializer.Serialize(new { willId, executorAccountId }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync();
            await _wallet.ReleaseFeeAsync(willId);
        }

        public async Task<HandoverResponse> AbortAsync(
            Guid willId, Guid adminAccountId)
        {
            var caseId = await _db.HandoverCases
                .Where(x => x.WillId == willId &&
                    (x.Status == "IN_PROGRESS" || x.Status == "PAUSED"))
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("No handover in progress.");

            var rows = await _db.HandoverCases
                .Where(x => x.Id == caseId &&
                    (x.Status == "IN_PROGRESS" || x.Status == "PAUSED"))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "ABORTED")
                    .SetProperty(x => x.CompletedAt, DateTimeOffset.UtcNow));

            if (rows == 0)
                throw new WillConflictException("Handover is no longer in progress.");

            await _db.Wills
                .Where(x => x.Id == willId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "ACTIVE")
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = adminAccountId, Action = "HANDOVER_ABORTED",
                EntityType = "HandoverCase",
                EntityId = caseId,
                Details = System.Text.Json.JsonSerializer.Serialize(new { willId, reason = "ADMIN_ABORT" }),
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync();
            await _wallet.RefundFeeAsync(willId);

            return await BuildResponseAsync(caseId);
        }

        // ---------------------------------------------------------------
        // H06: pause / resume / revise / reassign (dispute & failure path)
        // ---------------------------------------------------------------
        public async Task<HandoverResponse> PauseAsync(
            Guid willId, Guid actorAccountId, string reason)
        {
            var caseId = await _db.HandoverCases
                .Where(x => x.WillId == willId && x.Status == "IN_PROGRESS")
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("No handover in progress.");

            var rows = await _db.HandoverCases
                .Where(x => x.Id == caseId && x.Status == "IN_PROGRESS")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "PAUSED"));

            if (rows == 0)
                throw new WillConflictException("Handover is no longer in progress.");

            AddAudit(actorAccountId, "HANDOVER_PAUSED", "HandoverCase",
                caseId, new { willId, reason });
            await _db.SaveChangesAsync();

            return await BuildResponseAsync(caseId);
        }

        public async Task<HandoverResponse> ResumeAsync(
            Guid willId, Guid adminAccountId, bool resolvingDispute = false)
        {
            if (!resolvingDispute &&
                await _db.PaymentDisputes.AnyAsync(x =>
                    x.WillId == willId && x.Status == "OPEN"))
            {
                throw new WillConflictException(
                    "Resolve the open payment dispute before resuming the handover.");
            }

            var caseId = await _db.HandoverCases
                .Where(x => x.WillId == willId && x.Status == "PAUSED")
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("No paused handover.");

            var rows = await _db.HandoverCases
                .Where(x => x.Id == caseId && x.Status == "PAUSED")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "IN_PROGRESS"));

            if (rows == 0)
                throw new WillConflictException("Handover is no longer paused.");

            AddAudit(adminAccountId, "HANDOVER_RESUMED", "HandoverCase",
                caseId, new { willId, resolvingDispute });
            await _db.SaveChangesAsync();

            // The last delivery may have landed while the case was paused.
            var pending = await _db.HandoverItems.CountAsync(x =>
                x.HandoverCaseId == caseId && x.Status == "PENDING");

            if (pending == 0)
            {
                var executorId = await _db.ExecutorAssignments
                    .Where(x => x.WillId == willId)
                    .Select(x => x.ExecutorAccountId)
                    .FirstAsync();

                await CompleteAsync(caseId, willId, executorId);
            }

            return await BuildResponseAsync(caseId);
        }

        public async Task<HandoverResponse> ReopenItemAsync(
            Guid itemId, Guid adminAccountId, string reason)
        {
            var item = await _db.HandoverItems
                .AsNoTracking()
                .Include(x => x.HandoverCase)
                .FirstOrDefaultAsync(x => x.Id == itemId)
                ?? throw new KeyNotFoundException("Handover item not found.");

            if (item.HandoverCase!.Status != "PAUSED")
                throw new WillConflictException(
                    "Items can be reopened only while the handover is paused.");

            var rows = await _db.HandoverItems
                .Where(x => x.Id == itemId && x.Status == "DELIVERED" &&
                            x.HandoverCase!.Status == "PAUSED")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, "PENDING")
                    .SetProperty(x => x.DeliveredAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.DeliveredByAccountId, (Guid?)null));

            if (rows == 0)
                throw new WillConflictException("Only a delivered item can be reopened.");

            AddAudit(adminAccountId, "HANDOVER_ITEM_REOPENED", "HandoverItem",
                itemId, new { willId = item.HandoverCase.WillId, reason });
            await _db.SaveChangesAsync();

            return await BuildResponseAsync(item.HandoverCaseId);
        }

        public async Task<ExecutorReassignmentResponse> ReassignExecutorAsync(
            Guid willId,
            Guid newExecutorAccountId,
            Guid adminAccountId,
            string reason)
        {
            var will = await _db.Wills
                .Include(x => x.ExecutorAssignment)
                .FirstOrDefaultAsync(x => x.Id == willId)
                ?? throw new KeyNotFoundException("Will not found.");

            var current = will.ExecutorAssignment
                ?? throw new InvalidOperationException(
                    "This will has no executor assignment.");

            if (await _db.HandoverCases.AnyAsync(x =>
                    x.WillId == willId && x.Status == "IN_PROGRESS"))
            {
                throw new WillConflictException(
                    "Pause the handover before reassigning the executor.");
            }

            var pausedCaseId = await _db.HandoverCases
                .Where(x => x.WillId == willId && x.Status == "PAUSED")
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            if (pausedCaseId == null && will.Status != "ACTIVE")
            {
                throw new WillConflictException(
                    "An executor can be reassigned only for an ACTIVE will or while its handover is paused.");
            }

            if (newExecutorAccountId == will.OwnerAccountId)
                throw new InvalidOperationException(
                    "Owner cannot be the executor of their own will.");

            if (newExecutorAccountId == current.ExecutorAccountId)
                throw new InvalidOperationException(
                    "Select a different executor.");

            var profile = await _db.ExecutorProfiles
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.AccountId == newExecutorAccountId);

            if (profile == null ||
                !profile.IsVerified ||
                profile.IsSuspended ||
                profile.Account == null ||
                !string.Equals(profile.Account.Role, "EXECUTOR",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Selected account is not a verified executor.");
            }

            // Pre-flight: refund of the old hold + new hold must fit the wallet.
            var held = await _db.ServicePayments
                .Where(x => x.WillId == willId && x.Status == "HELD")
                .Select(x => x.Amount)
                .FirstOrDefaultAsync();

            var balance = await _db.Wallets
                .Where(x => x.AccountId == will.OwnerAccountId)
                .Select(x => x.Balance)
                .FirstOrDefaultAsync();

            if (balance + held < profile.ServiceFee)
            {
                throw new InvalidOperationException(
                    "The Owner wallet cannot cover the new executor's fee.");
            }

            if (pausedCaseId != null)
            {
                // Delivered items stay on record in the aborted case; the new
                // executor starts a fresh handover once the will is ACTIVE.
                var aborted = await _db.HandoverCases
                    .Where(x => x.Id == pausedCaseId && x.Status == "PAUSED")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, "ABORTED")
                        .SetProperty(x => x.CompletedAt, DateTimeOffset.UtcNow));

                if (aborted == 0)
                    throw new WillConflictException(
                        "Handover changed; refresh and retry.");
            }

            var oldExecutorId = current.ExecutorAccountId;

            await _wallet.RefundFeeAsync(willId);
            await _wallet.HoldFeeAsync(
                willId,
                will.OwnerAccountId,
                profile.AccountId,
                profile.ServiceFee,
                profile.Currency);

            var now = DateTimeOffset.UtcNow;
            current.ExecutorAccountId = profile.AccountId;
            current.Status = "PENDING";
            current.AssignedAt = now;
            current.RespondedAt = null;
            current.Scope = "WILL";
            current.ServiceFeeSnapshot = profile.ServiceFee;
            current.CurrencySnapshot = profile.Currency;

            will.Status = "PENDING_EXECUTOR";
            will.UpdatedAt = now;

            AddAudit(adminAccountId, "EXECUTOR_REASSIGNED", "Will", willId,
                new
                {
                    oldExecutorAccountId = oldExecutorId,
                    newExecutorAccountId = profile.AccountId,
                    fee = profile.ServiceFee,
                    currency = profile.Currency,
                    reason
                });
            await _db.SaveChangesAsync();

            return new ExecutorReassignmentResponse
            {
                WillId = willId,
                WillStatus = will.Status,
                ExecutorAccountId = profile.AccountId,
                ExecutorName = profile.Account!.FullName,
                AssignmentStatus = current.Status,
                ServiceFeeSnapshot = current.ServiceFeeSnapshot,
                CurrencySnapshot = current.CurrencySnapshot
            };
        }

        private void AddAudit(
            Guid? actorId, string action, string entityType,
            Guid? entityId, object details)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = actorId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = System.Text.Json.JsonSerializer.Serialize(details),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private async Task<HandoverResponse> BuildResponseAsync(
            Guid caseId, Guid? onlyBeneficiaryAccount = null)
        {
            var c = await _db.HandoverCases
                .AsNoTracking()
                .Include(x => x.Items)
                    .ThenInclude(i => i.AssetAssignment)
                        .ThenInclude(a => a!.WillAsset)
                            .ThenInclude(wa => wa!.Asset)
                .Include(x => x.Items)
                    .ThenInclude(i => i.AssetAssignment)
                        .ThenInclude(a => a!.BeneficiaryDesignation)
                .AsSplitQuery()
                .FirstAsync(x => x.Id == caseId);

            var items = c.Items
                .Where(i => onlyBeneficiaryAccount == null ||
                    i.AssetAssignment?.BeneficiaryDesignation
                        ?.BeneficiaryAccountId == onlyBeneficiaryAccount)
                .Select(i => new HandoverItemResponse
                {
                    Id = i.Id,
                    AssetAssignmentId = i.AssetAssignmentId,
                    AssetName = i.AssetAssignment?.WillAsset?.Asset?.Name
                        ?? string.Empty,
                    BeneficiaryName = i.AssetAssignment
                        ?.BeneficiaryDesignation?.FullName ?? string.Empty,
                    Relationship = i.AssetAssignment
                        ?.BeneficiaryDesignation?.Relationship,
                    AllocationPercentage = i.AssetAssignment
                        ?.AllocationPercentage ?? 0m,
                    Status = i.Status,
                    DeliveredAt = i.DeliveredAt
                })
                .ToList();

            return new HandoverResponse
            {
                Id = c.Id,
                WillId = c.WillId,
                Type = c.Type,
                Status = c.Status,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,
                TotalItems = items.Count,
                DeliveredItems = items.Count(x => x.Status == "DELIVERED"),
                Items = items
            };
        }
    }
}
