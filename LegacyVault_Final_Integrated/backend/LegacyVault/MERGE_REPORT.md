# Merge Report - LegacyVault backend

## Source

Base: project `LegacyVault - Copy.zip` do user upload.

## Giữ nguyên

IdentityAccess hiện tại được giữ nguyên về nghiệp vụ:
Register / Email OTP / Login / JWT / `/accounts/me` / account search.

## File shared thay đổi

### `Program.cs`

Thêm DI:

- `ICurrentUserService`
- `IAssetRepository`
- `IAssetEncryptionService`
- `IAssetService`
- `IWillRepository`
- `IWillService`

Giữ JWT setup hiện tại.

### `Data/LegacyVaultDbContext.cs`

Giữ mapping `Accounts` + `EmailOtps`, bổ sung:

- ExecutorProfiles
- Assets
- AssetSecrets
- AssetVerifications
- MockExternalAccounts
- Wills
- WillAssets
- BeneficiaryDesignations
- AssetAssignments
- ExecutorAssignments
- DmsConfigurations
- DmsCheckIns

## Kiệt

Added:
- `Shared/Security`
- `Features/AssetManagement`
- Asset-related models

Security:
- owner scope lấy từ JWT
- secret encrypted trước DB
- list/detail không trả secret

## Cường

Added:
- `Features/WillManagement`
- Will-related models

Business checks:
- only DRAFT can be modified
- selected Asset must belong to current owner
- linked/manual Beneficiary
- assignment entities must belong to same Will
- Executor must be verified EXECUTOR
- Submit requires Asset + Beneficiary + Assignment + Executor + DMS

## Runtime validation status

Static source merge completed.

This environment does not contain the .NET SDK, so `dotnet build` could not be executed here.

Therefore run Build Solution on your Visual Studio before claiming Asset/Will runtime PASS.
