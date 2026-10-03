namespace SWP301BETASK.Models
{
    public class AssetSecret
    {
        public Guid AssetId { get; set; }

        // Dữ liệu Asset đã mã hóa
        public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();

        public byte[] DataNonce { get; set; } = Array.Empty<byte>();

        public byte[] DataTag { get; set; } = Array.Empty<byte>();


        // DEK đã được Master Key / KEK mã hóa
        public byte[]? EncryptedDek { get; set; }

        public byte[]? DekNonce { get; set; }

        public byte[]? DekTag { get; set; }


        // Ví dụ: AES-256-GCM
        public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

        // Phiên bản format/cơ chế encryption
        public int EncryptionVersion { get; set; } = 1;

        // Master Key / KEK version đã dùng để wrap DEK
        public int KekVersion { get; set; } = 1;


        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        public Asset Asset { get; set; } = null!;
    }
}