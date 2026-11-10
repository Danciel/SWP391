using System.Security.Cryptography;
using System.Text;

namespace LegacyVault.Features.WillManagement.Services
{
    // C8: death certificates are encrypted at rest (AES-256-GCM).
    // File layout:  "LVC1" | nonce (12) | tag (16) | ciphertext
    // The legal case id is bound to the file as associated data, so a file
    // copied under another case id fails to decrypt.
    // The key is derived (HKDF) from the same Encryption:KekBase64 secret the
    // asset vault already uses, so no new secret has to be deployed.
    public static class CertificateCrypto
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LVC1");
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public static byte[] DeriveKey(string kekBase64)
        {
            byte[] kek;
            try { kek = Convert.FromBase64String(kekBase64); }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "Encryption:KekBase64 must be valid Base64.");
            }

            return HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                kek,
                32,
                salt: null,
                info: Encoding.UTF8.GetBytes("legacyvault/death-certificate/v1"));
        }

        public static byte[] Encrypt(byte[] key, Guid caseId, byte[] plain)
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipher = new byte[plain.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plain, cipher, tag, caseId.ToByteArray());

            var blob = new byte[Magic.Length + NonceSize + TagSize + cipher.Length];
            Buffer.BlockCopy(Magic, 0, blob, 0, Magic.Length);
            Buffer.BlockCopy(nonce, 0, blob, Magic.Length, NonceSize);
            Buffer.BlockCopy(tag, 0, blob, Magic.Length + NonceSize, TagSize);
            Buffer.BlockCopy(cipher, 0, blob, Magic.Length + NonceSize + TagSize, cipher.Length);
            return blob;
        }

        public static byte[] Decrypt(byte[] key, Guid caseId, byte[] blob)
        {
            var header = Magic.Length + NonceSize + TagSize;

            if (blob.Length < header ||
                !blob.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            {
                throw new InvalidOperationException(
                    "The stored certificate is not in the expected format.");
            }

            var nonce = blob.AsSpan(Magic.Length, NonceSize);
            var tag = blob.AsSpan(Magic.Length + NonceSize, TagSize);
            var cipher = blob.AsSpan(header);
            var plain = new byte[cipher.Length];

            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, cipher, tag, plain, caseId.ToByteArray());
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException(
                    "The stored certificate failed its integrity check.");
            }

            return plain;
        }

        // The file must really be what its extension says (no .exe renamed to .pdf).
        public static bool MatchesExtension(string extension, byte[] content)
        {
            if (content.Length < 8) return false;

            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                    return content[0] == 0x25 && content[1] == 0x50 &&
                           content[2] == 0x44 && content[3] == 0x46 &&
                           content[4] == 0x2D; // %PDF-
                case ".jpg":
                case ".jpeg":
                    return content[0] == 0xFF && content[1] == 0xD8 &&
                           content[2] == 0xFF;
                case ".png":
                    return content[0] == 0x89 && content[1] == 0x50 &&
                           content[2] == 0x4E && content[3] == 0x47 &&
                           content[4] == 0x0D && content[5] == 0x0A &&
                           content[6] == 0x1A && content[7] == 0x0A;
                default:
                    return false;
            }
        }
    }
}
