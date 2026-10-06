namespace SWP301BETASK.Models
{
    public class DmsCheckIn
    {
        public Guid Id { get; set; }

        // Thuộc cấu hình DMS nào
        public Guid DmsConfigurationId { get; set; }

        // Thời điểm hệ thống kỳ vọng user check-in
        public DateTime ScheduledAt { get; set; }

        // Thời điểm user thực sự check-in
        public DateTime? CheckedInAt { get; set; }

        // PENDING / COMPLETED / MISSED
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public DmsConfiguration DmsConfiguration { get; set; } = null!;
    }
}