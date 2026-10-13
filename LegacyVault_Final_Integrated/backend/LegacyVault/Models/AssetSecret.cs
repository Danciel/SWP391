namespace LegacyVault.Models
{
    public class AssetSecret
    {
        public Guid AssetId { get; set; }

        public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();

        public byte[] DataNonce { get; set; } = Array.Empty<byte>();

        public byte[] DataTag { get; set; } = Array.Empty<byte>();

        public byte[] EncryptedDek { get; set; } = Array.Empty<byte>();

        public byte[] DekNonce { get; set; } = Array.Empty<byte>();

        public byte[] DekTag { get; set; } = Array.Empty<byte>();

        public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

        public int EncryptionVersion { get; set; } = 1;

        public int KekVersion { get; set; } = 1;

        public DateTimeOffset UpdatedAt { get; set; }

        public Asset? Asset { get; set; }
    }
}
