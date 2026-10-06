using System.Security.Cryptography;
using System.Text.Json;
using SWP301BETASK.Features.AssetManagement.Contracts;

namespace SWP301BETASK.Features.AssetManagement.Services
{
    public class EncryptionService : IEncryptionService
    {
        private const int DekSize = 32;     // 256 bit
        private const int NonceSize = 12;   // AES-GCM recommended
        private const int TagSize = 16;     // 128 bit

        private readonly IConfiguration _configuration;

        public EncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // =====================================================
        // ENCRYPT ASSET
        // =====================================================

        public AssetEncryptionResult EncryptAsset<T>(
            T data,
            Guid assetId,
            Guid ownerAccountId)
        {
            // 1. Serialize object thành JSON byte[]
            byte[] plainText =
                JsonSerializer.SerializeToUtf8Bytes(data);

            // 2. Tạo DEK riêng cho Asset
            byte[] dek =
                RandomNumberGenerator.GetBytes(DekSize);

            // 3. Tạo AAD để bind ciphertext với Asset + Owner
            byte[] aad =
                CreateAad(assetId, ownerAccountId);

            // 4. Lấy KEK hiện tại
            int kekVersion =
                GetCurrentKekVersion();

            byte[] kek =
                GetKek(kekVersion);

            try
            {
                // =============================================
                // LAYER 1:
                // Encrypt Asset data bằng DEK
                // =============================================

                byte[] dataNonce =
                    RandomNumberGenerator.GetBytes(NonceSize);

                byte[] encryptedPayload =
                    new byte[plainText.Length];

                byte[] dataTag =
                    new byte[TagSize];

                using (var aesData =
                       new AesGcm(dek, TagSize))
                {
                    aesData.Encrypt(
                        dataNonce,
                        plainText,
                        encryptedPayload,
                        dataTag,
                        aad);
                }


                // =============================================
                // LAYER 2:
                // Encrypt / Wrap DEK bằng KEK
                // =============================================

                byte[] dekNonce =
                    RandomNumberGenerator.GetBytes(NonceSize);

                byte[] encryptedDek =
                    new byte[dek.Length];

                byte[] dekTag =
                    new byte[TagSize];

                using (var aesDek =
                       new AesGcm(kek, TagSize))
                {
                    aesDek.Encrypt(
                        dekNonce,
                        dek,
                        encryptedDek,
                        dekTag,
                        aad);
                }


                // 5. Trả toàn bộ dữ liệu cần lưu DB
                return new AssetEncryptionResult
                {
                    EncryptedPayload = encryptedPayload,

                    DataNonce = dataNonce,

                    DataTag = dataTag,

                    EncryptedDek = encryptedDek,

                    DekNonce = dekNonce,

                    DekTag = dekTag,

                    EncryptionAlgorithm = "AES-256-GCM",

                    EncryptionVersion = 1,

                    KekVersion = kekVersion
                };
            }
            finally
            {
                // Xóa dữ liệu nhạy cảm tạm khỏi buffer
                CryptographicOperations.ZeroMemory(plainText);
                CryptographicOperations.ZeroMemory(dek);
                CryptographicOperations.ZeroMemory(kek);
            }
        }


        // =====================================================
        // DECRYPT ASSET
        // =====================================================

        public T DecryptAsset<T>(
            byte[] encryptedPayload,
            byte[] dataNonce,
            byte[] dataTag,
            byte[] encryptedDek,
            byte[] dekNonce,
            byte[] dekTag,
            int kekVersion,
            Guid assetId,
            Guid ownerAccountId)
        {
            byte[] aad =
                CreateAad(assetId, ownerAccountId);

            // Lấy đúng KEK version đã dùng lúc encrypt
            byte[] kek =
                GetKek(kekVersion);

            byte[] dek =
                new byte[DekSize];

            byte[] plainText =
                new byte[encryptedPayload.Length];

            try
            {
                // =============================================
                // LAYER 1:
                // Unwrap DEK bằng KEK
                // =============================================

                using (var aesDek =
                       new AesGcm(kek, TagSize))
                {
                    aesDek.Decrypt(
                        dekNonce,
                        encryptedDek,
                        dekTag,
                        dek,
                        aad);
                }


                // =============================================
                // LAYER 2:
                // Decrypt Asset data bằng DEK
                // =============================================

                using (var aesData =
                       new AesGcm(dek, TagSize))
                {
                    aesData.Decrypt(
                        dataNonce,
                        encryptedPayload,
                        dataTag,
                        plainText,
                        aad);
                }


                // Convert JSON byte[] về object
                var result =
                    JsonSerializer.Deserialize<T>(plainText);

                if (result == null)
                {
                    throw new InvalidOperationException(
                        "Unable to deserialize decrypted asset data.");
                }

                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(kek);
                CryptographicOperations.ZeroMemory(dek);
                CryptographicOperations.ZeroMemory(plainText);
            }
        }


        // =====================================================
        // CREATE AAD
        // =====================================================

        private static byte[] CreateAad(
            Guid assetId,
            Guid ownerAccountId)
        {
            byte[] aad = new byte[32];

            assetId.TryWriteBytes(
                aad.AsSpan(0, 16));

            ownerAccountId.TryWriteBytes(
                aad.AsSpan(16, 16));

            return aad;
        }


        // =====================================================
        // CURRENT KEK VERSION
        // =====================================================

        private int GetCurrentKekVersion()
        {
            string? value =
                _configuration[
                    "Encryption:CurrentKekVersion"];

            if (!int.TryParse(value, out int version)
                || version <= 0)
            {
                throw new InvalidOperationException(
                    "Encryption CurrentKekVersion is missing or invalid.");
            }

            return version;
        }


        // =====================================================
        // LOAD KEK
        // =====================================================

        private byte[] GetKek(int version)
        {
            string? keyBase64 =
                _configuration[
                    $"Encryption:Keys:{version}"];

            if (string.IsNullOrWhiteSpace(keyBase64))
            {
                throw new InvalidOperationException(
                    $"KEK version {version} was not found.");
            }

            byte[] key;

            try
            {
                key =
                    Convert.FromBase64String(keyBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    $"KEK version {version} is not valid Base64.",
                    ex);
            }

            if (key.Length != 32)
            {
                CryptographicOperations.ZeroMemory(key);

                throw new InvalidOperationException(
                    $"KEK version {version} must be exactly 32 bytes.");
            }

            return key;
        }
    }
}