# LegacyVault - Merged Backend: Kiệt + Cường

Bản này được merge trực tiếp từ project backend mà bạn upload.

## Những phần cũ được giữ nguyên

- Account model hiện tại.
- EmailOtp model hiện tại.
- Register USER.
- BCrypt password.
- Gmail OTP thật.
- Verify / Resend OTP.
- Login.
- JWT.
- `GET /api/accounts/me`.
- exact search email / phone + JWT + masked response.

Không thay lại flow Auth đang chạy tốt.

## Phần mới của Kiệt

`Features/AssetManagement`

- Asset CRUD.
- Owner lấy từ JWT.
- AES-256-GCM envelope encryption.
- Mỗi Asset có random 32-byte DEK riêng.
- KEK/Master Key mã hóa DEK.
- DataNonce/DataTag.
- DekNonce/DekTag.
- AAD = `AssetId + OwnerAccountId`.
- Secret không xuất hiện ở list/detail.
- `/api/assets/{id}/secret` dùng để chứng minh decrypt thành công.
- `/api/assets/{id}/verify` đối chiếu `MockExternalAccounts`.
- Ghi lịch sử vào `AssetVerifications`.

## Phần mới của Cường

`Features/WillManagement`

Flow:

`DRAFT -> Add Asset -> Beneficiary -> Assignment -> Executor -> DMS -> Review -> Submit -> PENDING_EXECUTOR`

Có:

- CRUD Will.
- chỉ add Asset thuộc owner.
- Beneficiary liên kết account trong hệ thống.
- Beneficiary manual cho người chưa có account.
- Asset Assignment.
- list/select verified Executor.
- DMS configuration.
- DMS check-in.
- submit validation.

## Database

Project này tiếp tục dùng schema Supabase hiện tại làm source-of-truth.

Không thêm `EnsureCreated()`.
Không tự chạy `Database.Migrate()` khi startup.

DbContext đã được merge mapping cho các table hiện có.

## Việc phải làm trước khi test Asset

Project upload không chứa User Secrets. Tuy nhiên `UserSecretsId` của `.csproj` được giữ nguyên, vì vậy Supabase/JWT/Gmail secrets hiện có trên máy bạn vẫn được dùng nếu bạn chạy trên cùng máy/user profile.

Bạn chỉ cần thêm KEK mới:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Copy output, rồi:

```powershell
dotnet user-secrets set "Encryption:KekBase64" "PASTE_BASE64_HERE"
```

`Encryption:KekVersion = 1` đã nằm trong `appsettings.json`.

Không dùng `Jwt:Key` làm KEK.

## Test

1. Build Solution.
2. Run backend.
3. `GET /api/health/database`.
4. Mở `LegacyVault.http`.
5. Login trước để lấy JWT.
6. Test Asset theo thứ tự.
7. Test Will theo thứ tự.

## Frontend

Chưa sửa frontend Nhật trong bản backend này.

Sau khi backend build + test API ổn, bước tiếp theo là lấy frontend Nhật và đồng bộ:

- Role `OWNER` -> `USER` cho public account.
- Register không gửi role.
- Auth routes theo backend hiện tại.
- `MOCK_MODE` chuyển sang hybrid/real theo từng feature.
- Assets gọi API thật.
- Create Will gọi API thật.
- 1 input search beneficiary bằng email hoặc phone.
- Phone OTP / FaceID / third-party Identity vẫn mock ở phase hiện tại.
