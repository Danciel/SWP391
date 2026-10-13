using SWP301BETASK.Features.WillManagement.DTOs;
using SWP301BETASK.Features.WillManagement.Models;

namespace SWP301BETASK.Features.WillManagement.Services
{
    internal static class WillMapper
    {
        public static BeneficiaryDto ToDto(BeneficiaryDesignation b) =>
            new(b.Id, b.BeneficiaryAccountId, b.FullName, b.Email, b.Phone, b.IdentityNumber, b.Relationship);

        public static ExecutorDto ToDto(ExecutorProfile p) =>
            new(p.Id, p.Name, p.Specialization, p.License, p.Rating, p.Reviews, p.ExperienceYears, p.CompletedCases, p.Fee, p.Available);

        public static WillDto ToDto(Will w)
        {
            var assets = w.Assignments.OrderBy(a => a.AssignedAt)
                .Select(a => new WillAssetDto(a.AssetId, a.AssignmentStatus, a.HandoverStatus,
                    a.Beneficiary == null ? null : ToDto(a.Beneficiary)))
                .ToList();

            var d = w.Dms;
            var dms = new DmsDto(true, d?.IntervalDays ?? 30, d?.ReminderDays ?? 3, d?.MaxMissed ?? 2,
                d?.Status ?? DmsStatuses.Waiting, d?.MissedCount ?? 0, d?.LastCheckInAt, d?.NextDueAt);

            var ex = w.Executor;
            var payment = new PaymentDto(ex?.PaymentStatus ?? PaymentStatuses.None, ex?.Fee ?? 0);
            var hire = ex?.RequestedAt == null ? null : new HireRequestDto(ex.Id, ex.Status, ex.RequestedAt, ex.DeadlineAt);

            return new WillDto(w.Id, w.Name, w.Description, w.Status, assets,
                w.Beneficiaries.Select(ToDto).ToList(),
                ex == null ? null : ToDto(ex.Profile), dms, payment, hire, w.CreatedAt, w.UpdatedAt);
        }
    }
}
