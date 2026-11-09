using LegacyVault.Models;

namespace LegacyVault.Features.AssetManagement.Contracts
{
    public interface IAssetEncryptionService
    {
        AssetSecret Encrypt(
            Guid assetId,
            Guid ownerAccountId,
            string plaintext);

        string Decrypt(
            Guid assetId,
            Guid ownerAccountId,
            AssetSecret secret);
    }
}
