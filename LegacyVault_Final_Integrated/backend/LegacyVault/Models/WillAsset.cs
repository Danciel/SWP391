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

        // Optional specialist for this asset only (null = the Will's executor).
        public Guid? ExecutorAccountId { get; set; }

        public Account? ExecutorAccount { get; set; }

        public ICollection<AssetAssignment> Assignments { get; set; }
            = new List<AssetAssignment>();
    }
}
