using SWP301BETASK.Features.AssetManagement.Contracts;
using SWP301BETASK.Features.AssetManagement.DTOs;
using SWP301BETASK.Models;

namespace SWP301BETASK.Features.AssetManagement.Services
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _repository;
        private readonly IEncryptionService _encryptionService;
        private readonly IAssetVerificationService _verificationService;

        public AssetService(
            IAssetRepository repository,
            IEncryptionService encryptionService,
            IAssetVerificationService verificationService)
        {
            _repository = repository;
            _encryptionService = encryptionService;
            _verificationService = verificationService;
        }

        // =====================================================
        // GET ALL
        // =====================================================

        public async Task<List<AssetResponse>> GetAllAsync(
            Guid accountId)
        {
            var assets =
                await _repository.GetAllByOwnerAsync(accountId);

            return assets
                .Select(asset => ToResponse(asset, null))
                .ToList();
        }


        // =====================================================
        // GET BY ID
        // =====================================================

        public async Task<AssetResponse?> GetByIdAsync(
            Guid accountId,
            Guid assetId)
        {
            var asset =
                await _repository.GetByIdAsync(
                    assetId,
                    accountId);

            if (asset == null)
            {
                return null;
            }

            AssetSecretPayload? payload = null;

            if (asset.Secret != null)
            {
                payload = DecryptSecret(
                    asset,
                    accountId);
            }

            return ToResponse(
                asset,
                payload);
        }


        // =====================================================
        // GET SECRET / REVEAL PASSWORD
        // =====================================================

        public async Task<AssetSecretResponse?> GetSecretAsync(
            Guid accountId,
            Guid assetId)
        {
            var asset =
                await _repository.GetByIdAsync(
                    assetId,
                    accountId);

            if (asset?.Secret == null)
            {
                return null;
            }

            var payload =
                DecryptSecret(
                    asset,
                    accountId);

            return new AssetSecretResponse
            {
                Username = payload.Username,
                Password = payload.Password,
                Note = payload.Note
            };
        }


        // =====================================================
        // CREATE
        // =====================================================

        public async Task<AssetResponse> CreateAsync(
            Guid accountId,
            CreateAssetRequest request)
        {
            // 1. Kiểm tra Account tồn tại
            bool accountExists =
                await _repository
                    .AccountExistsAsync(accountId);

            if (!accountExists)
            {
                throw new KeyNotFoundException(
                    "Account does not exist.");
            }


            // 2. Chuẩn hóa dữ liệu
            string assetType =
                request.AssetType
                    .Trim()
                    .ToUpperInvariant();

            string? provider =
                request.Provider?
                    .Trim()
                    .ToUpperInvariant();

            string verificationStatus =
                "UNVERIFIED";


            // 3. Nếu là Online Account thì verify trước
            if (assetType == "ACCOUNT")
            {
                if (string.IsNullOrWhiteSpace(provider) ||
                    string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    throw new InvalidOperationException(
                        "Provider, username and password are required for ACCOUNT assets.");
                }

                bool verified =
                    await _verificationService.VerifyAsync(
                        provider,
                        request.Username,
                        request.Password);

                if (!verified)
                {
                    throw new InvalidOperationException(
                        "External account verification failed.");
                }

                verificationStatus = "VERIFIED";
            }


            // 4. Tạo AssetId trước
            // Vì AssetId được dùng làm AAD
            Guid assetId =
                Guid.NewGuid();


            // 5. Gom dữ liệu nhạy cảm
            var payload =
                new AssetSecretPayload
                {
                    Username = request.Username,
                    Password = request.Password,
                    Note = request.Note
                };


            // 6. Encrypt:
            // Payload -> DEK -> Ciphertext
            // DEK -> KEK -> EncryptedDEK
            var encrypted =
                _encryptionService.EncryptAsset(
                    payload,
                    assetId,
                    accountId);


            // 7. Tạo Asset metadata
            var asset =
                new Asset
                {
                    Id = assetId,

                    OwnerAccountId = accountId,

                    Name = request.Name.Trim(),

                    AssetType = assetType,

                    Provider = provider,

                    Status = "ACTIVE",

                    VerificationStatus =
                        verificationStatus,

                    CreatedAt = DateTime.UtcNow,

                    UpdatedAt = DateTime.UtcNow
                };


            // 8. Lưu encrypted secret
            asset.Secret =
                new AssetSecret
                {
                    AssetId = assetId,

                    EncryptedPayload =
                        encrypted.EncryptedPayload,

                    DataNonce =
                        encrypted.DataNonce,

                    DataTag =
                        encrypted.DataTag,

                    EncryptedDek =
                        encrypted.EncryptedDek,

                    DekNonce =
                        encrypted.DekNonce,

                    DekTag =
                        encrypted.DekTag,

                    EncryptionAlgorithm =
                        encrypted.EncryptionAlgorithm,

                    EncryptionVersion =
                        encrypted.EncryptionVersion,

                    KekVersion =
                        encrypted.KekVersion,

                    UpdatedAt =
                        DateTime.UtcNow
                };


            // 9. Nếu verify thành công thì lưu lịch sử
            if (verificationStatus == "VERIFIED")
            {
                asset.Verifications.Add(
                    new AssetVerification
                    {
                        Id = Guid.NewGuid(),

                        AssetId = assetId,

                        Provider = provider!,

                        ExternalIdentifier =
                            request.Username,

                        Status = "VERIFIED",

                        VerifiedAt =
                            DateTime.UtcNow,

                        CreatedAt =
                            DateTime.UtcNow
                    });
            }


            // 10. Save DB
            await _repository.AddAsync(asset);


            // 11. Response không trả password
            return ToResponse(
                asset,
                payload);
        }


        // =====================================================
        // UPDATE
        // =====================================================

        public async Task<AssetResponse?> UpdateAsync(
            Guid accountId,
            Guid assetId,
            UpdateAssetRequest request)
        {
            var asset =
                await _repository.GetByIdAsync(
                    assetId,
                    accountId);

            if (asset == null)
            {
                return null;
            }


            // 1. Decrypt dữ liệu hiện tại
            AssetSecretPayload payload;

            if (asset.Secret != null)
            {
                payload =
                    DecryptSecret(
                        asset,
                        accountId);
            }
            else
            {
                payload =
                    new AssetSecretPayload();
            }


            // 2. Xác định giá trị mới
            string newAssetType =
                request.AssetType?
                    .Trim()
                    .ToUpperInvariant()
                ?? asset.AssetType;

            string? newProvider =
                request.Provider?
                    .Trim()
                    .ToUpperInvariant()
                ?? asset.Provider;

            string? newUsername =
                request.Username
                ?? payload.Username;

            string? newPassword =
                request.Password
                ?? payload.Password;

            string? newNote =
                request.Note
                ?? payload.Note;


            // 3. Credential có thay đổi không?
            bool credentialsChanged =
                request.AssetType != null ||
                request.Provider != null ||
                request.Username != null ||
                request.Password != null;


            // 4. Nếu ACCOUNT và credential thay đổi
            // -> verify lại
            if (newAssetType == "ACCOUNT"
                && credentialsChanged)
            {
                if (string.IsNullOrWhiteSpace(newProvider) ||
                    string.IsNullOrWhiteSpace(newUsername) ||
                    string.IsNullOrWhiteSpace(newPassword))
                {
                    throw new InvalidOperationException(
                        "Provider, username and password are required for ACCOUNT assets.");
                }

                bool verified =
                    await _verificationService.VerifyAsync(
                        newProvider,
                        newUsername,
                        newPassword);

                if (!verified)
                {
                    throw new InvalidOperationException(
                        "External account verification failed.");
                }

                asset.VerificationStatus =
                    "VERIFIED";

                asset.Verifications.Add(
                    new AssetVerification
                    {
                        Id = Guid.NewGuid(),

                        AssetId = asset.Id,

                        Provider = newProvider,

                        ExternalIdentifier =
                            newUsername,

                        Status = "VERIFIED",

                        VerifiedAt =
                            DateTime.UtcNow,

                        CreatedAt =
                            DateTime.UtcNow
                    });
            }


            // 5. Update metadata
            if (!string.IsNullOrWhiteSpace(
                request.Name))
            {
                asset.Name =
                    request.Name.Trim();
            }

            asset.AssetType =
                newAssetType;

            asset.Provider =
                newProvider;

            if (!string.IsNullOrWhiteSpace(
                request.Status))
            {
                asset.Status =
                    request.Status
                        .Trim()
                        .ToUpperInvariant();
            }


            // 6. Update plaintext payload trong RAM
            payload.Username =
                newUsername;

            payload.Password =
                newPassword;

            payload.Note =
                newNote;


            // 7. Encrypt lại bằng DEK mới
            var encrypted =
                _encryptionService.EncryptAsset(
                    payload,
                    asset.Id,
                    accountId);


            // 8. Nếu chưa có Secret thì tạo
            if (asset.Secret == null)
            {
                asset.Secret =
                    new AssetSecret
                    {
                        AssetId = asset.Id
                    };
            }


            // 9. Ghi ciphertext mới
            asset.Secret.EncryptedPayload =
                encrypted.EncryptedPayload;

            asset.Secret.DataNonce =
                encrypted.DataNonce;

            asset.Secret.DataTag =
                encrypted.DataTag;

            asset.Secret.EncryptedDek =
                encrypted.EncryptedDek;

            asset.Secret.DekNonce =
                encrypted.DekNonce;

            asset.Secret.DekTag =
                encrypted.DekTag;

            asset.Secret.EncryptionAlgorithm =
                encrypted.EncryptionAlgorithm;

            asset.Secret.EncryptionVersion =
                encrypted.EncryptionVersion;

            asset.Secret.KekVersion =
                encrypted.KekVersion;

            asset.Secret.UpdatedAt =
                DateTime.UtcNow;

            asset.UpdatedAt =
                DateTime.UtcNow;


            // 10. Save
            await _repository.SaveChangesAsync();


            return ToResponse(
                asset,
                payload);
        }


        // =====================================================
        // DELETE
        // =====================================================

        public async Task<bool> DeleteAsync(
            Guid accountId,
            Guid assetId)
        {
            var asset =
                await _repository.GetByIdAsync(
                    assetId,
                    accountId);

            if (asset == null)
            {
                return false;
            }

            await _repository.DeleteAsync(asset);

            return true;
        }


        // =====================================================
        // VERIFY ONLY
        // =====================================================

        public async Task<VerifyAssetResponse> VerifyAsync(
            VerifyAssetRequest request)
        {
            bool verified =
                await _verificationService.VerifyAsync(
                    request.Provider,
                    request.Username,
                    request.Password);

            return new VerifyAssetResponse
            {
                Verified = verified,

                Message = verified
                    ? "Asset verified successfully."
                    : "Asset verification failed."
            };
        }


        // =====================================================
        // DECRYPT HELPER
        // =====================================================

        private AssetSecretPayload DecryptSecret(
            Asset asset,
            Guid ownerAccountId)
        {
            var secret =
                asset.Secret
                ?? throw new InvalidOperationException(
                    "Asset secret does not exist.");


            if (secret.EncryptedDek == null ||
                secret.DekNonce == null ||
                secret.DekTag == null)
            {
                throw new InvalidOperationException(
                    "Asset encryption metadata is incomplete.");
            }


            return _encryptionService
                .DecryptAsset<AssetSecretPayload>(
                    secret.EncryptedPayload,
                    secret.DataNonce,
                    secret.DataTag,
                    secret.EncryptedDek,
                    secret.DekNonce,
                    secret.DekTag,
                    secret.KekVersion,
                    asset.Id,
                    ownerAccountId);
        }


        // =====================================================
        // MAPPING
        // =====================================================

        private static AssetResponse ToResponse(
            Asset asset,
            AssetSecretPayload? payload)
        {
            return new AssetResponse
            {
                Id = asset.Id,

                Name = asset.Name,

                AssetType =
                    asset.AssetType,

                Provider =
                    asset.Provider,

                VerificationStatus =
                    asset.VerificationStatus,

                Status =
                    asset.Status,

                Username =
                    payload?.Username,

                Note =
                    payload?.Note,

                HasEncryptedSecret =
                    asset.Secret != null,

                CreatedAt =
                    asset.CreatedAt,

                UpdatedAt =
                    asset.UpdatedAt
            };
        }
    }
}