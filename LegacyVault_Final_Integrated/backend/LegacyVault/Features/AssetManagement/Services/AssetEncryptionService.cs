using System.Security.Cryptography;
using System.Text;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Models;

namespace LegacyVault.Features.AssetManagement.Services
{
    public class AssetEncryptionService : IAssetEncryptionService
    {
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly IConfiguration _configuration;

        public AssetEncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AssetSecret Encrypt(
            Guid assetId,
            Guid ownerAccountId,
            string plaintext)
        {
            var kek = GetKek();
            var dek = RandomNumberGenerator.GetBytes(KeySize);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var dataNonce = RandomNumberGenerator.GetBytes(NonceSize);
            var dataTag = new byte[TagSize];
            var encryptedPayload = new byte[plaintextBytes.Length];
            var aad = BuildAad(assetId, ownerAccountId);

            try
            {
                using (var dataAes = new AesGcm(dek, TagSize))
                {
                    dataAes.Encrypt(
                        dataNonce,
                        plaintextBytes,
                        encryptedPayload,
                        dataTag,
                        aad);
                }

                var dekNonce = RandomNumberGenerator.GetBytes(NonceSize);
                var dekTag = new byte[TagSize];
                var encryptedDek = new byte[dek.Length];

                using (var dekAes = new AesGcm(kek, TagSize))
                {
                    dekAes.Encrypt(
                        dekNonce,
                        dek,
                        encryptedDek,
                        dekTag,
                        aad);
                }

                return new AssetSecret
                {
                    AssetId = assetId,
                    EncryptedPayload = encryptedPayload,
                    DataNonce = dataNonce,
                    DataTag = dataTag,
                    EncryptedDek = encryptedDek,
                    DekNonce = dekNonce,
                    DekTag = dekTag,
                    EncryptionAlgorithm = "AES-256-GCM",
                    EncryptionVersion = 1,
                    KekVersion = GetKekVersion(),
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dek);
                CryptographicOperations.ZeroMemory(plaintextBytes);
                CryptographicOperations.ZeroMemory(kek);
            }
        }

        public string Decrypt(
            Guid assetId,
            Guid ownerAccountId,
            AssetSecret secret)
        {
            if (!string.Equals(
                secret.EncryptionAlgorithm,
                "AES-256-GCM",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Unsupported encryption algorithm.");
            }

            var kek = GetKek();
            var dek = new byte[KeySize];
            var aad = BuildAad(assetId, ownerAccountId);

            try
            {
                using (var dekAes = new AesGcm(kek, TagSize))
                {
                    dekAes.Decrypt(
                        secret.DekNonce,
                        secret.EncryptedDek,
                        secret.DekTag,
                        dek,
                        aad);
                }

                var plaintextBytes =
                    new byte[secret.EncryptedPayload.Length];

                try
                {
                    using (var dataAes = new AesGcm(dek, TagSize))
                    {
                        dataAes.Decrypt(
                            secret.DataNonce,
                            secret.EncryptedPayload,
                            secret.DataTag,
                            plaintextBytes,
                            aad);
                    }

                    return Encoding.UTF8.GetString(plaintextBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintextBytes);
                }
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException(
                    "Unable to decrypt asset secret.");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dek);
                CryptographicOperations.ZeroMemory(kek);
            }
        }

        private byte[] GetKek()
        {
            var kekBase64 =
                _configuration["Encryption:KekBase64"];

            if (string.IsNullOrWhiteSpace(kekBase64))
            {
                throw new InvalidOperationException(
                    "Encryption:KekBase64 is not configured.");
            }

            byte[] kek;

            try
            {
                kek = Convert.FromBase64String(kekBase64);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "Encryption:KekBase64 must be valid Base64.");
            }

            if (kek.Length != KeySize)
            {
                CryptographicOperations.ZeroMemory(kek);

                throw new InvalidOperationException(
                    "Encryption KEK must be exactly 32 bytes.");
            }

            return kek;
        }

        private int GetKekVersion()
        {
            var value =
                _configuration["Encryption:KekVersion"];

            return int.TryParse(value, out var version)
                ? version
                : 1;
        }

        private static byte[] BuildAad(
            Guid assetId,
            Guid ownerAccountId)
        {
            return Encoding.UTF8.GetBytes(
                $"{assetId:N}:{ownerAccountId:N}");
        }
    }
}
