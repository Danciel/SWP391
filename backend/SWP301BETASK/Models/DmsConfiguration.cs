namespace SWP301BETASK.Models
{
    public class DmsConfiguration
    {
        public Guid Id { get; set; }

        // Mỗi Will có 1 cấu hình DMS
        public Guid WillId { get; set; }

        public bool Enabled { get; set; } = true;

        // Bao nhiêu ngày thì cần check-in một lần
        public int CheckInIntervalDays { get; set; }

        // Nhắc trước bao nhiêu ngày
        public int ReminderBeforeDays { get; set; }

        // Số lần bỏ lỡ tối đa trước khi trigger
        public int MaxMissedCheckIns { get; set; }

        // Số lần hiện tại đã bỏ lỡ
        public int MissedCheckIns { get; set; } = 0;

        public DateTime? LastCheckInAt { get; set; }

        public DateTime? NextCheckInAt { get; set; }

        // WAITING / ACTIVE / TRIGGERED / PAUSED / CLOSED
        public string Status { get; set; } = "WAITING";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public Will Will { get; set; } = null!;
    }
}