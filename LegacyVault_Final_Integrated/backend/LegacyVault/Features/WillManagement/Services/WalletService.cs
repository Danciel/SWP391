using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using LegacyVault.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // Simulated money only. Hold -> (handover verified) -> release.
    public class WalletService : IWalletService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly ICurrentUserService _user;

        public WalletService(
            LegacyVaultDbContext db,
            ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public async Task<WalletResponse> GetMineAsync()
        {
            var me = _user.GetRequiredAccountId();
            var wallet = await GetOrCreateAsync(me);

            var held = await _db.ServicePayments
                .Where(x => x.OwnerAccountId == me && x.Status == "HELD")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var incoming = await _db.ServicePayments
                .Where(x => x.ExecutorAccountId == me && x.Status == "HELD")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var executorDepositHeld = await _db.ExecutorDeposits.AsNoTracking()
                .Where(x => x.ExecutorAccountId == me && x.Status == "HELD")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var txs = await _db.WalletTransactions
                .AsNoTracking()
                .Where(x => x.AccountId == me)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new WalletTransactionResponse
                {
                    Id = x.Id,
                    Type = x.Type,
                    Amount = x.Amount,
                    BalanceAfter = x.BalanceAfter,
                    WillId = x.WillId,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            // Read again: tracked entity may be stale after ExecuteUpdate.
            var balance = await _db.Wallets
                .AsNoTracking()
                .Where(x => x.AccountId == me)
                .Select(x => x.Balance)
                .FirstAsync();

            return new WalletResponse
            {
                Balance = balance,
                HeldAmount = held,
                PendingIncoming = incoming,
                ExecutorDepositHeld = executorDepositHeld,
                Currency = wallet.Currency,
                Transactions = txs
            };
        }

        public async Task<WalletResponse> TopUpAsync(decimal amount, string idempotencyKey)
        {
            var me = _user.GetRequiredAccountId();
            await GetOrCreateAsync(me);

            // A retry with the same key returns the current wallet without crediting twice.
            var prior = await _db.WalletTransactions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == me && x.IdempotencyKey == idempotencyKey);
            if (prior != null)
            {
                if (prior.Type != "TOPUP" || prior.Amount != amount)
                    throw new InvalidOperationException("This Idempotency-Key was already used for a different top-up request.");
                return await GetMineAsync();
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                await ChangeBalanceAsync(me, amount, "TOPUP", null, "Mock top-up", idempotencyKey);
                AddAudit(me, "WALLET_TOPUP", "Wallet", me,
                    new { amount, currency = "VND", mode = "MOCK", idempotencyKey });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                // Concurrent duplicate requests race on the unique key; only one can credit.
                var duplicate = await _db.WalletTransactions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AccountId == me && x.IdempotencyKey == idempotencyKey);
                if (duplicate != null)
                {
                    if (duplicate.Type == "TOPUP" && duplicate.Amount == amount)
                        return await GetMineAsync();
                    throw new InvalidOperationException("This Idempotency-Key was already used for a different top-up request.");
                }
                throw;
            }

            return await GetMineAsync();
        }

        public async Task HoldExecutorDepositAsync(Guid willId, Guid executorAccountId, decimal serviceFee, string currency)
        {
            // This is an advance paid from the Owner's held service fee, not a deposit
            // deducted from the Executor. It is credited only after the Executor accepts.
            if (executorAccountId == Guid.Empty || willId == Guid.Empty)
                return;

            var existing = await _db.ExecutorDeposits.FirstOrDefaultAsync(x => x.WillId == willId);
            if (existing != null) return;

            var amount = Math.Round(serviceFee * 0.20m, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0m) return;

            await GetOrCreateAsync(executorAccountId);
            await using var tx = await _db.Database.BeginTransactionAsync();

            // Re-check within the transaction so an accepted assignment retry won't credit twice.
            if (await _db.ExecutorDeposits.AnyAsync(x => x.WillId == willId))
            {
                await tx.RollbackAsync();
                return;
            }

            var rows = await _db.Wallets.Where(x => x.AccountId == executorAccountId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Balance, x => x.Balance + amount)
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));
            if (rows == 0) throw new InvalidOperationException("Executor wallet was not found.");

            var balance = await _db.Wallets.AsNoTracking().Where(x => x.AccountId == executorAccountId)
                .Select(x => x.Balance).FirstAsync();
            var now = DateTimeOffset.UtcNow;
            _db.ExecutorDeposits.Add(new ExecutorDeposit { Id = Guid.NewGuid(), WillId = willId,
                ExecutorAccountId = executorAccountId, Amount = amount, Currency = currency, Status = "HELD", HeldAt = now });
            _db.WalletTransactions.Add(new WalletTransaction { Id = Guid.NewGuid(), AccountId = executorAccountId,
                Type = "ADVANCE_IN", Amount = amount, BalanceAfter = balance, WillId = willId,
                Note = $"20% service-fee advance for Will {willId}", CreatedAt = now });
            var profile = await _db.ExecutorProfiles.FirstOrDefaultAsync(x => x.AccountId == executorAccountId);
            if (profile != null) profile.TotalHeldDeposit += amount;
            _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorAccountId = executorAccountId,
                Action = "EXECUTOR_ADVANCE_CREDITED", EntityType = "ExecutorDeposit", EntityId = willId,
                Details = System.Text.Json.JsonSerializer.Serialize(new { amount, currency, serviceFee, percent = 20 }), CreatedAt = now });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task HoldFeeAsync(
            Guid willId,
            Guid ownerAccountId,
            Guid executorAccountId,
            decimal amount,
            string currency)
        {
            if (amount <= 0m) return;

            var existing = await _db.ServicePayments
                .FirstOrDefaultAsync(x => x.WillId == willId);

            // A settle may have changed the row through ExecuteUpdate in this
            // same request; never trust a stale tracked copy.
            if (existing != null)
                await _db.Entry(existing).ReloadAsync();

            if (existing != null && existing.Status == "HELD") return;

            await GetOrCreateAsync(ownerAccountId);

            await using var tx = await _db.Database.BeginTransactionAsync();

            // Atomic: only subtract when the balance is enough.
            var rows = await _db.Wallets
                .Where(x => x.AccountId == ownerAccountId &&
                            x.Balance >= amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance - amount)
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));

            if (rows == 0)
            {
                throw new InvalidOperationException(
                    "Insufficient wallet balance to hold the executor fee.");
            }

            var balance = await _db.Wallets
                .AsNoTracking()
                .Where(x => x.AccountId == ownerAccountId)
                .Select(x => x.Balance)
                .FirstAsync();

            var now = DateTimeOffset.UtcNow;

            AddTx(ownerAccountId, "HOLD", -amount, balance, willId,
                "Executor fee held");

            if (existing == null)
            {
                _db.ServicePayments.Add(new ServicePayment
                {
                    Id = Guid.NewGuid(),
                    WillId = willId,
                    OwnerAccountId = ownerAccountId,
                    ExecutorAccountId = executorAccountId,
                    Amount = amount,
                    Currency = currency,
                    Status = "HELD",
                    HeldAt = now
                });
            }
            else
            {
                // Re-hold after an earlier refund.
                existing.ExecutorAccountId = executorAccountId;
                existing.Amount = amount;
                existing.Currency = currency;
                existing.Status = "HELD";
                existing.HeldAt = now;
                existing.RefundedAt = null;
                existing.ReleasedAt = null;
            }

            AddAudit(ownerAccountId, "PAYMENT_HELD", "ServicePayment", willId,
                new { amount, currency, executorAccountId });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public Task ReleaseFeeAsync(Guid willId, bool ignoreOpenDispute = false)
            => SettleAsync(willId, release: true, ignoreOpenDispute);

        public Task RefundFeeAsync(Guid willId)
            => SettleAsync(willId, release: false, ignoreOpenDispute: true);

        private async Task SettleAsync(Guid willId, bool release, bool ignoreOpenDispute)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var now = DateTimeOffset.UtcNow;
            var newStatus = release ? "RELEASED" : "REFUNDED";
            DateTimeOffset? releasedAt = release ? now : null;
            DateTimeOffset? refundedAt = release ? null : now;

            // Atomic status flip: only one caller can settle a HELD payment.
            // H06/F05: a release never passes while a dispute is OPEN.
            var flipped = await _db.ServicePayments
                .Where(x => x.WillId == willId && x.Status == "HELD" &&
                    (ignoreOpenDispute ||
                     !_db.PaymentDisputes.Any(d =>
                         d.WillId == willId && d.Status == "OPEN")))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, newStatus)
                    .SetProperty(x => x.ReleasedAt, releasedAt)
                    .SetProperty(x => x.RefundedAt, refundedAt));

            if (flipped == 0)
            {
                await tx.RollbackAsync();

                if (release && !ignoreOpenDispute)
                {
                    var blocked =
                        await _db.ServicePayments.AnyAsync(x =>
                            x.WillId == willId && x.Status == "HELD") &&
                        await _db.PaymentDisputes.AnyAsync(d =>
                            d.WillId == willId && d.Status == "OPEN");

                    if (blocked)
                    {
                        Guid? blockedActor;
                        try { blockedActor = _user.GetRequiredAccountId(); }
                        catch { blockedActor = null; }
                        AddAudit(blockedActor, "PAYMENT_RELEASE_BLOCKED_BY_DISPUTE",
                            "ServicePayment", willId, new { willId });
                        await _db.SaveChangesAsync();
                    }
                }

                return;
            }

            var payment = await _db.ServicePayments
                .AsNoTracking()
                .FirstAsync(x => x.WillId == willId);

            var target = release
                ? payment.ExecutorAccountId
                : payment.OwnerAccountId;

            await GetOrCreateAsync(target);

            await ChangeBalanceAsync(
                target,
                payment.Amount,
                release ? "RELEASE_IN" : "REFUND",
                willId,
                release ? "Fee released after verified handover"
                        : "Held fee refunded");

            Guid? actorId;
            try { actorId = _user.GetRequiredAccountId(); }
            catch { actorId = null; }
            AddAudit(actorId, release ? "PAYMENT_RELEASED" : "PAYMENT_REFUNDED",
                "ServicePayment", payment.Id,
                new { willId, payment.Amount, payment.Currency, targetAccountId = target });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task ChangeBalanceAsync(
            Guid accountId,
            decimal delta,
            string type,
            Guid? willId,
            string note,
            string? idempotencyKey = null)
        {
            await _db.Wallets
                .Where(x => x.AccountId == accountId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance + delta)
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));

            var balance = await _db.Wallets
                .AsNoTracking()
                .Where(x => x.AccountId == accountId)
                .Select(x => x.Balance)
                .FirstAsync();

            AddTx(accountId, type, delta, balance, willId, note, idempotencyKey);
            await _db.SaveChangesAsync();
        }

        private void AddTx(
            Guid accountId,
            string type,
            decimal amount,
            decimal balanceAfter,
            Guid? willId,
            string note,
            string? idempotencyKey = null)
        {
            _db.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Type = type,
                Amount = amount,
                BalanceAfter = balanceAfter,
                WillId = willId,
                IdempotencyKey = idempotencyKey,
                Note = note,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private void AddAudit(Guid? actorId, string action, string entityType, Guid? entityId, object details)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(), ActorAccountId = actorId, Action = action,
                EntityType = entityType, EntityId = entityId,
                Details = System.Text.Json.JsonSerializer.Serialize(details),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private async Task<Wallet> GetOrCreateAsync(Guid accountId)
        {
            var wallet = await _db.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == accountId);

            if (wallet != null) return wallet;

            wallet = new Wallet
            {
                AccountId = accountId,
                Balance = 0m,
                Currency = "VND",
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Wallets.Add(wallet);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Created concurrently by another request.
                _db.Entry(wallet).State = EntityState.Detached;
                wallet = await _db.Wallets
                    .AsNoTracking()
                    .FirstAsync(x => x.AccountId == accountId);
            }

            _db.Entry(wallet).State = EntityState.Detached;
            return wallet;
        }
    }
}
