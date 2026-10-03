namespace SWP301BETASK.Features.AssetManagement.Contracts
{
    public interface IEncryptionService
    {
        AssetEncryptionResult EncryptAsset<T>(
            T data,
            Guid assetId,
            Guid ownerAccountId);

        T DecryptAsset<T>(
            byte[] encryptedPayload,
            byte[] dataNonce,
            byte[] dataTag,
            byte[] encryptedDek,
            byte[] dekNonce,
            byte[] dekTag,
            int kekVersion,
            Guid assetId,
            Guid ownerAccountId);
    }

    public class AssetEncryptionResult
    {
        public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();

        public byte[] DataNonce { get; set; } = Array.Empty<byte>();

        public byte[] DataTag { get; set; } = Array.Empty<byte>();

        public byte[] EncryptedDek { get; set; } = Array.Empty<byte>();

        public byte[] DekNonce { get; set; } = Array.Empty<byte>();

        public byte[] DekTag { get; set; } = Array.Empty<byte>();

        public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

        public int EncryptionVersion { get; set; } = 1;

        public int KekVersion { get; set; }
    }
}