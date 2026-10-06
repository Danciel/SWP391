namespace SWP301BETASK.Models
{
    public class WillAsset
    {
        public Guid Id { get; set; }

        // Will nào đang chứa Asset này
        public Guid WillId { get; set; }

        // Asset nào được đưa vào Will
        public Guid AssetId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public Will Will { get; set; } = null!;

        public Asset Asset { get; set; } = null!;
    }
}