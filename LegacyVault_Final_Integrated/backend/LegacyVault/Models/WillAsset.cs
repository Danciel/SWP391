namespace LegacyVault.Models
{
    public class WillAsset
    {
        public Guid Id { get; set; }

        public Guid WillId { get; set; }

        public Guid AssetId { get; set; }

        public DateTimeOffset AddedAt { get; set; }

        public Will? Will { get; set; }

        public Asset? Asset { get; set; }

        public ICollection<AssetAssignment> Assignments { get; set; }
            = new List<AssetAssignment>();
    }
}
