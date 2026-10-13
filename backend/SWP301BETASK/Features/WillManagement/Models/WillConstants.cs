namespace SWP301BETASK.Features.WillManagement.Models
{
    public static class WillStatus
    {
        public const string Draft = "DRAFT";
        public const string PendingExecutorAcceptance = "PENDING_EXECUTOR_ACCEPTANCE";
        public const string Active = "ACTIVE";
        public const string NeedsExecutor = "NEEDS_EXECUTOR";
        public const string PendingVerification = "PENDING_VERIFICATION";
        public const string PendingHandoverReview = "PENDING_HANDOVER_REVIEW";
        public const string HandedOver = "HANDED_OVER";
    }

    public static class AssignmentStatuses
    {
        public const string Active = "ACTIVE";
        public const string Unassigned = "UNASSIGNED"; // asset is in the will but has no beneficiary (yet / any more)
        public const string Revoked = "REVOKED";
        public const string Transferred = "TRANSFERRED";
    }

    public static class HandoverStatuses
    {
        public const string Pending = "PENDING";
        public const string Ready = "READY";
        public const string Accessed = "ACCESSED";
        public const string Completed = "COMPLETED";
    }

    public static class ExecutorAssignmentStatuses
    {
        public const string Selected = "SELECTED"; // chosen in draft, hire request not sent yet
        public const string Pending = "PENDING";   // waiting for executor to accept
        public const string Accepted = "ACCEPTED";
        public const string Declined = "DECLINED";
    }

    public static class PaymentStatuses
    {
        public const string None = "NONE";
        public const string Held = "HELD";
        public const string Paid = "PAID";
        public const string Released = "RELEASED";
    }

    public static class DmsStatuses
    {
        public const string Waiting = "WAITING"; // will not active yet
        public const string Active = "ACTIVE";
        public const string Triggered = "TRIGGERED";
        public const string Paused = "PAUSED";
    }
}
