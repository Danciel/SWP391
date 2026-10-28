using System.Text.Json;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.AssetManagement.DTOs;
using LegacyVault.Models;
using LegacyVault.Shared.Security;

namespace LegacyVault.Features.AssetManagement.Services
{
    public class AssetService : IAssetService
    {
        private static readonly HashSet<string> AllowedAssetTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "ACCOUNT",
                "DOCUMENT",
                "DIGITAL_WALLET",
                "CLOUD_STORAGE",
                "BANK_ACCOUNT",
                "OTHER"
            };

        private readonly IAssetRepository _assetRepository;
        private readonly IAssetEncryptionService _encryptionService;
        private readonly ICurrentUserService _currentUserService;

        public AssetService(
            IAssetRepository assetRepository,
            IAssetEncryptionService encryptionService,
            ICurrentUserService currentUserService)
        {
            _assetRepository = assetRepository;
            _encryptionService = encryptionService;
            _currentUserService = currentUserService;
        }

        public async Task<AssetResponse> CreateAsync(
            CreateAssetRequest request)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var assetType =
                NormalizeAssetType(request.AssetType);

            if (request.SecretData == null ||
                request.SecretData.Count == 0)
            {
                throw new InvalidOperationException(
                    "SecretData is required.");
            }

            var now = DateTimeOffset.UtcNow;

            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                OwnerAccountId = ownerId,
                Name = request.Name.Trim(),
                Description =
                    string.IsNullOrWhiteSpace(request.Description)
                        ? null
                        : request.Description.Trim(),
                AssetType = assetType,
                Provider =
                    string.IsNullOrWhiteSpace(request.Provider)
                        ? null
                        : request.Provider.Trim().ToUpperInvariant(),
                Status = "ACTIVE",
                VerificationStatus = "UNVERIFIED",
                CreatedAt = now,
                UpdatedAt = now
            };

            var secretJson =
                JsonSerializer.Serialize(request.SecretData);

            var secret =
                _encryptionService.Encrypt(
                    asset.Id,
                    ownerId,
                    secretJson);

            await _assetRepository.AddAsync(asset);
            await _assetRepository.AddSecretAsync(secret);
            await _assetRepository.SaveChangesAsync();

            return ToResponse(asset);
        }

        public async Task<List<AssetResponse>> GetMineAsync()
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var assets =
                await _assetRepository
                    .GetOwnedAssetsAsync(ownerId);

            return assets.Select(ToResponse).ToList();
        }

        public async Task<AssetResponse> GetByIdAsync(
            Guid assetId)
        {
            var asset =
                await GetOwnedAssetRequiredAsync(
                    assetId,
                    tracking: false);

            return ToResponse(asset);
        }

        public async Task<AssetResponse> UpdateAsync(
            Guid assetId,
            UpdateAssetRequest request)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var asset =
                await _assetRepository.GetOwnedAssetAsync(
                    assetId,
                    ownerId,
                    tracking: true);

            if (asset == null)
            {
                throw new KeyNotFoundException(
                    "Asset not found.");
            }

            // An OAuth-verified identity cannot be replaced by editing account metadata.
            var requestedType = NormalizeAssetType(request.AssetType);
            var requestedProvider = string.IsNullOrWhiteSpace(request.Provider)
                ? null : request.Provider.Trim().ToUpperInvariant();
            if (!string.Equals(asset.AssetType, requestedType, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(asset.Provider, requestedProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Asset type and provider cannot be changed. Connect a new asset instead.");
            }

            asset.Name = request.Name.Trim();
            asset.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null : request.Description.Trim();
            asset.UpdatedAt = DateTimeOffset.UtcNow;

            bool oauthAccount = asset.AssetType == "ACCOUNT"
                && (asset.Provider is "GMAIL" or "GITHUB");
            // Never replace OAuth proof and encrypted secrets with arbitrary JSON via Update.
            if (request.SecretData != null && oauthAccount)
                throw new InvalidOperationException("Verified account secrets cannot be replaced directly.");

            if (!string.IsNullOrEmpty(request.HandoverPassword))
            {
                if (!oauthAccount)
                    throw new InvalidOperationException("Password handover applies only to Gmail or GitHub.");
                if (!request.HandoverConsent)
                    throw new InvalidOperationException("Confirm your authorization to store the password.");

                var currentSecret = await _assetRepository.GetSecretAsync(assetId, tracking: true);
                if (currentSecret == null)
                    throw new InvalidOperationException("Asset secret not found.");

                var existingJson = _encryptionService.Decrypt(assetId, ownerId, currentSecret);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson)
                    ?? throw new InvalidOperationException("Stored asset secret is invalid.");
                data["handoverPassword"] = request.HandoverPassword;
                data["handoverPasswordVerified"] = "false";
                var replacement = _encryptionService.Encrypt(assetId, ownerId, JsonSerializer.Serialize(data));
                CopyEncryptedSecret(replacement, currentSecret);
                // OAuth verified identity stays unchanged: this password is not OAuth verified.
            }
            else if (request.SecretData != null)
            {
                if (request.SecretData.Count == 0)
                    throw new InvalidOperationException("SecretData cannot be empty.");
                var currentSecret = await _assetRepository.GetSecretAsync(assetId, tracking: true);
                if (currentSecret == null)
                    throw new InvalidOperationException("Asset secret not found.");
                var replacement = _encryptionService.Encrypt(
                    assetId, ownerId, JsonSerializer.Serialize(request.SecretData));
                CopyEncryptedSecret(replacement, currentSecret);
                asset.VerificationStatus = "UNVERIFIED";
            }

            await _assetRepository.SaveChangesAsync();

            return ToResponse(asset);
        }

        public async Task DeleteAsync(Guid assetId)
        {
            var asset =
                await GetOwnedAssetRequiredAsync(
                    assetId,
                    tracking: true);

            _assetRepository.Remove(asset);
            await _assetRepository.SaveChangesAsync();
        }

        public async Task<AssetSecretResponse> RevealSecretAsync(
            Guid assetId)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var asset =
                await _assetRepository.GetOwnedAssetAsync(
                    assetId,
                    ownerId,
                    tracking: false);

            if (asset == null)
            {
                throw new KeyNotFoundException(
                    "Asset not found.");
            }

            var secret =
                await _assetRepository.GetSecretAsync(
                    assetId,
                    tracking: false);

            if (secret == null)
            {
                throw new KeyNotFoundException(
                    "Asset secret not found.");
            }

            var json =
                _encryptionService.Decrypt(
                    asset.Id,
                    ownerId,
                    secret);

            var secretData =
                JsonSerializer.Deserialize<
                    Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();

            return new AssetSecretResponse
            {
                AssetId = asset.Id,
                SecretData = secretData
            };
        }

        public async Task<AssetVerificationResponse> VerifyAsync(
            Guid assetId)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var asset =
                await _assetRepository.GetOwnedAssetAsync(
                    assetId,
                    ownerId,
                    tracking: true);

            if (asset == null)
            {
                throw new KeyNotFoundException(
                    "Asset not found.");
            }

            if (string.IsNullOrWhiteSpace(asset.Provider))
            {
                throw new InvalidOperationException(
                    "Asset provider is required for verification.");
            }

            if (asset.Provider is "GMAIL" or "GITHUB" or "GOOGLE_DRIVE" or "LOCAL_DOCUMENT")
            {
                throw new InvalidOperationException(
                    "Use provider OAuth or document integrity verification; mock credential verification is disabled.");
            }

            var secret =
                await _assetRepository.GetSecretAsync(
                    assetId,
                    tracking: false);

            if (secret == null)
            {
                throw new KeyNotFoundException(
                    "Asset secret not found.");
            }

            var json =
                _encryptionService.Decrypt(
                    asset.Id,
                    ownerId,
                    secret);

            var raw =
                JsonSerializer.Deserialize<
                    Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();

            var normalized =
                raw.ToDictionary(
                    x => x.Key.ToLowerInvariant(),
                    x => x.Value);

            normalized.TryGetValue(
                "username",
                out var username);

            normalized.TryGetValue(
                "password",
                out var password);

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Verification requires secret keys 'username' and 'password'.");
            }

            var external =
                await _assetRepository
                    .FindMockExternalAccountAsync(
                        asset.Provider,
                        username);

            var verified =
                external != null &&
                external.Password == password;

            var now = DateTimeOffset.UtcNow;

            asset.VerificationStatus =
                verified ? "VERIFIED" : "FAILED";
            asset.UpdatedAt = now;

            var verification =
                new AssetVerification
                {
                    Id = Guid.NewGuid(),
                    AssetId = asset.Id,
                    Provider = asset.Provider,
                    ExternalIdentifier = username,
                    Status =
                        verified ? "VERIFIED" : "FAILED",
                    FailureReason =
                        verified
                            ? null
                            : "Mock provider credentials did not match.",
                    VerifiedAt =
                        verified ? now : null,
                    CreatedAt = now
                };

            await _assetRepository
                .AddVerificationAsync(verification);

            await _assetRepository.SaveChangesAsync();

            return new AssetVerificationResponse
            {
                AssetId = asset.Id,
                Status = asset.VerificationStatus,
                Message =
                    verified
                        ? "Asset verified successfully."
                        : "Asset verification failed."
            };
        }

        private async Task<Asset> GetOwnedAssetRequiredAsync(
            Guid assetId,
            bool tracking)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var asset =
                await _assetRepository.GetOwnedAssetAsync(
                    assetId,
                    ownerId,
                    tracking);

            if (asset == null)
            {
                throw new KeyNotFoundException(
                    "Asset not found.");
            }

            return asset;
        }

        private static string NormalizeAssetType(
            string assetType)
        {
            var normalized =
                assetType.Trim().ToUpperInvariant();

            if (!AllowedAssetTypes.Contains(normalized))
            {
                throw new InvalidOperationException(
                    "Invalid asset type.");
            }

            return normalized;
        }

        private static AssetResponse ToResponse(Asset asset)
        {
            return new AssetResponse
            {
                Id = asset.Id,
                Name = asset.Name,
                Description = asset.Description,
                AssetType = asset.AssetType,
                Provider = asset.Provider,
                Status = asset.Status,
                VerificationStatus =
                    asset.VerificationStatus,
                CreatedAt = asset.CreatedAt,
                UpdatedAt = asset.UpdatedAt
            };
        }

        private static void CopyEncryptedSecret(
            AssetSecret source,
            AssetSecret destination)
        {
            destination.EncryptedPayload =
                source.EncryptedPayload;
            destination.DataNonce =
                source.DataNonce;
            destination.DataTag =
                source.DataTag;
            destination.EncryptedDek =
                source.EncryptedDek;
            destination.DekNonce =
                source.DekNonce;
            destination.DekTag =
                source.DekTag;
            destination.EncryptionAlgorithm =
                source.EncryptionAlgorithm;
            destination.EncryptionVersion =
                source.EncryptionVersion;
            destination.KekVersion =
                source.KekVersion;
            destination.UpdatedAt =
                source.UpdatedAt;
        }
    }
}
