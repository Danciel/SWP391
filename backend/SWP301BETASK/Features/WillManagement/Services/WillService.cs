using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Models;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Services
{
    public class WillService : IWillService
    {
        private readonly LegacyVaultDbContext _db;
        private readonly IAssetLookup _assets;

        public WillService(LegacyVaultDbContext db, IAssetLookup assets)
        {
            _db = db;
            _assets = assets;
        }

        public async Task<WillDto> CreateAsync(Guid ownerId, CreateWillRequest req, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var will = new Will
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = req.Name?.Trim() ?? string.Empty,
                Description = req.Description?.Trim() ?? string.Empty,
                Status = WillStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now,
                // DMS is mandatory and always on; defaults match the FE wizard.
                Dms = new DmsConfiguration { Id = Guid.NewGuid(), IntervalDays = 30, ReminderDays = 3, MaxMissed = 2, Status = DmsStatuses.Waiting }
            };
            _db.Wills.Add(will);
            await _db.SaveChangesAsync(ct);
            return WillMapper.ToDto(will);
        }

        public async Task<List<WillDto>> ListAsync(Guid ownerId, CancellationToken ct)
        {
            var wills = await _db.Wills.AsNoTracking()
                .Where(w => w.OwnerId == ownerId)
                .WithDetails()
                .OrderByDescending(w => w.UpdatedAt)
                .ToListAsync(ct);
            return wills.Select(WillMapper.ToDto).ToList();
        }

        public async Task<WillDto> GetAsync(Guid ownerId, Guid willId, CancellationToken ct) =>
            WillMapper.ToDto(await _db.LoadOwnedAsync(ownerId, willId, ct, tracking: false));

        public async Task<WillDto> UpdateAsync(Guid ownerId, Guid willId, UpdateWillRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            if (will.Status == WillStatus.HandedOver)
                throw ApiException.Conflict("A handed-over will can no longer be edited");

            if (req.Name != null) will.Name = req.Name.Trim();
            if (req.Description != null) will.Description = req.Description.Trim();
            return await SaveAsync(will, ct);
        }

        public async Task DeleteDraftAsync(Guid ownerId, Guid willId, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            if (will.Status != WillStatus.Draft)
                throw ApiException.Conflict("Only draft wills can be deleted");
            _db.Wills.Remove(will);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<WillDto> SetAssetsAsync(Guid ownerId, Guid willId, SetWillAssetsRequest req, CancellationToken ct)
        {
            var will = await _db.LoadOwnedAsync(ownerId, willId, ct);
            will.RequireStatus(WillRules.DraftOnly, "Assets can only be changed while the will is a draft");

            var wanted = req.AssetIds.Distinct().ToList();
            var owned = await _assets.GetOwnedAssetIdsAsync(ownerId, wanted, ct);
            var invalid = wanted.Where(id => !owned.Contains(id)).ToList();
            if (invalid.Count > 0)
                throw ApiException.BadRequest("Some assets do not exist or do not belong to you", invalid.Select(i => i.ToString()));

            // remove de-selected assets
            foreach (var a in will.Assignments.Where(a => !wanted.Contains(a.AssetId)).ToList())
            {
                will.Assignments.Remove(a);
                _db.AssetAssignments.Remove(a);
            }

            // add newly selected assets (existing rows are kept so their beneficiary stays)
            var now = DateTime.UtcNow;
            foreach (var assetId in wanted.Where(id => will.Assignments.All(a => a.AssetId != id)))
            {
                will.Assignments.Add(new AssetAssignment
                {
                    Id = Guid.NewGuid(),
                    WillId = will.Id,
                    AssetId = assetId,
                    AssignedByOwnerId = ownerId,
                    AssignedAt = now,
                    AssignmentStatus = AssignmentStatuses.Unassigned,
                    HandoverStatus = HandoverStatuses.Pending
                });
            }
            return await SaveAsync(will, ct);
        }

        private async Task<WillDto> SaveAsync(Will will, CancellationToken ct)
        {
            will.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return WillMapper.ToDto(will);
        }
    }
}
