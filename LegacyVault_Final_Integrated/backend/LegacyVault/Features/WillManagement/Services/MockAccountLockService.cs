using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Models;

namespace LegacyVault.Features.WillManagement.Services
{
    // MOCK for N7. Does NOT lock anything: it only records that a lock was
    // requested, so the hand-off point to Nhien is visible in the audit log.
    // Replace the registration in Program.cs with Nhien's real class.
    public class MockAccountLockService : IAccountLockService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly ILogger<MockAccountLockService> _logger;

        public MockAccountLockService(
            LegacyVaultDbContext db,
            ILogger<MockAccountLockService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LockOwnerAsync(
            Guid ownerAccountId,
            Guid legalCaseId,
            string reason,
            CancellationToken ct = default)
        {
            _logger.LogWarning(
                "MOCK account lock: owner {OwnerId} should be locked (legal case {CaseId}).",
                ownerAccountId, legalCaseId);

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = null,
                Action = "OWNER_LOCK_REQUESTED",
                EntityType = "Account",
                EntityId = ownerAccountId,
                Details = System.Text.Json.JsonSerializer.Serialize(
                    new { legalCaseId, reason, mock = true }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        public async Task UnlockOwnerAsync(
            Guid ownerAccountId,
            string reason,
            CancellationToken ct = default)
        {
            _logger.LogWarning(
                "MOCK account lock: owner {OwnerId} should be unlocked.",
                ownerAccountId);

            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorAccountId = null,
                Action = "OWNER_UNLOCK_REQUESTED",
                EntityType = "Account",
                EntityId = ownerAccountId,
                Details = System.Text.Json.JsonSerializer.Serialize(
                    new { reason, mock = true }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
