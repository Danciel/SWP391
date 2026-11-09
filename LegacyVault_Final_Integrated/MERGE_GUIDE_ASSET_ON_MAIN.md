# SWP391 MAIN + LegacyVault Asset OAuth (kiểm soát phạm vi thay đổi)

## Nguồn chuẩn
- **Base**: `SWP391-main.zip` (giữ nguyên phát triển Wallet, Handover, Identity, Executor của nhóm).
- **Source tham khảo**: `LegacyVault_Final_Integrated_Asset_Cleaned (2).zip` (OAuth Gmail/GitHub, Google Drive File ID, Document SHA-256, mật khẩu bàn giao).
- Không dùng bản Asset để ghi đè `WillService`, `WalletService`, `ExecutorInboxController`, `Identity`, `Migrations`, `CreateWill.jsx`, `Dashboard`.

## ĐÃ CODE vào Main (phần Asset)
1. Gmail/GitHub OAuth thật + so khớp Gmail/Username khai báo, Google Drive OAuth và File ID; PKCE/state/ticket.
2. Password for Handover (tùy chọn) mã hóa cùng `AssetSecrets` bằng mã hóa AES-256-GCM sẵn có; OAuth **không** xác minh Password này.
3. Document upload PDF/JPG/PNG <= 2 MB, SHA-256 integrity và download.
4. Edit Asset: tên, mô tả, mật khẩu bàn giao mới (để trống giữ mật khẩu cũ). Không sửa Provider/tài khoản đã xác minh; vẫn giữ `AssetId`/liên kết Will.
5. Xóa Asset chỉ khi không được `WillAssets` tham chiếu; xóa `AssetVerifications` và `AssetSecrets` của Asset này trong transaction.
6. Không thêm Mock Brokerage. Không đổi schema database hay thuật toán encryption.
7. Thay phần Asset trong `vaultApi.js`, giữ toàn bộ API mới của Main; giải quyết marker xung đột Git có sẵn ở phần Handover, ưu tiên hành vi `Updated upstream` của Main.
8. Tương thích form `AssetFields` / `AssetFileField` đang được `CreateWill.jsx` dùng. **Không sửa `CreateWill.jsx`.**
9. Tắt mock password-verification cho Gmail/GitHub/Google Drive ở route `/assets/validated` và `/assets/{id}/verify`, tránh gọi nhầm là xác minh Google thật.

## PHẠM VI FILE THAY ĐỔI (11)
- `backend/LegacyVault/Features/AssetManagement/Controllers/AssetConnectionsController.cs`
- `backend/LegacyVault/Features/AssetManagement/Controllers/AssetMaintenanceController.cs`
- `backend/LegacyVault/Features/AssetManagement/Controllers/DocumentController.cs`
- `backend/LegacyVault/Features/AssetManagement/Controllers/ValidatedAssetsController.cs`
- `backend/LegacyVault/Features/AssetManagement/DTOs/UpdateAssetRequest.cs`
- `backend/LegacyVault/Features/AssetManagement/Services/AssetService.cs`
- `backend/LegacyVault/Program.cs`
- `frontend/src/api/assetConnectionApi.js`
- `frontend/src/api/vaultApi.js`
- `frontend/src/pages/Assets/Assets.jsx`
- `frontend/src/utils/assetCatalog.js`

## MÀN HÌNH / CHỨC NĂNG CỦA NHÓM KHÔNG ĐƯỢC THAY ĐỔI
- WillService, WillsController, WillDetails, MyWills, CreateWill, WalletService, ExecutorInbox, HandoverService, IdentityAccess, database migrations.
- **`Program.cs` là file dùng chung bắt buộc thêm đúng `AddMemoryCache`, `AddHttpClient("asset-oauth")`, cho CORS trên 5174.** Cần review kỹ diff của file này.
- **`vaultApi.js` là file dùng chung**, chỉ thay hàm Asset, giữ các API khác, và sửa 1 đoạn conflict markers đã có sẵn trong Main.

## THIẾT LẬP CẦN CÓ Ở MÁY DEV, KHÔNG COMMIT SECRET
- Trong User Secrets ASP.NET: `OAuth:GMAIL:ClientId`, `OAuth:GMAIL:ClientSecret`, `OAuth:GITHUB:ClientId`, `OAuth:GITHUB:ClientSecret`, `OAuth:GOOGLE_DRIVE:ClientId`, `OAuth:GOOGLE_DRIVE:ClientSecret` (có thể dùng cùng thông tin OAuth của Google nhưng phải cấu hình cả hai keys; giữ scopes và callback hợp lệ), tùy chọn `OAuth:CallbackBaseUrl=https://localhost:7015`.
- Google Console phải khai báo redirect `/api/assets/connections/gmail/callback`, `/google_drive/callback`; GitHub callback `/api/assets/connections/github/callback`. Khi thay domain/port phải cập nhật console.
- Asset AES KEK cũ của Main phải được giữ nguyên. Tuyệt đối không đổi key giữa các lần chạy vì dữ liệu đã mã hóa có thể không giải được.
- Frontend Vite `http://localhost:5173` hoặc `:5174`; backend HTTPS `https://localhost:7015`. Popup và cookie của OAuth dùng HTTPS backend.
- Provider OAuth không trả về mật khẩu đăng nhập thực tế. Không test bằng mật khẩu thật.
- Google Drive `drive.file` chỉ truy cập được các file app được người dùng cấp quyền phù hợp; File ID bất kỳ không luôn hợp lệ.

## KIỂM THỬ NGHIỆM THU
- Frontend: `cd frontend && npm ci && npm run build`.
- Backend (máy có .NET 10 SDK): `dotnet build backend/LegacyVault.slnx`.
- Gmail nhập A -> OAuth A được; nhập A -> OAuth B từ chối; đăng nhập lại A tạo Asset trùng trả 409.
- GitHub username khai báo và username OAuth phải khớp.
- Edit password giữ `AssetId`, `VerificationStatus`, `WillAsset` tham chiếu nguyên vẹn. Edit rỗng không xóa password cũ.
- Delete Asset tự do; Delete Asset trong Will trả 409; dùng Document upload/download/hash.
- Verify giả lập không đánh dấu Gmail/GitHub thật là VERIFIED từ username/password.
- OAuth status `PROVIDER_VERIFIED` / `FILE_ACCESS_VERIFIED`, Document `INTEGRITY_VERIFIED`: nếu DB production có CHECK status cũ, cần DBA kiểm tra và tạo migration **riêng qua review**, không tự phá constraint.
- Xác minh khả năng build/backend thật và API end-to-end trên môi trường nhóm, bản này chưa kiểm thử .NET tại container.

## CHƯA CODE vào MAIN — PHẢI REVIEW CÙNG OWNER MODULE
1. **Will Detail PIN 6 số**: Cần Backend chặn tất cả endpoint trả chi tiết Will (kể cả đường gián tiếp từ Dashboard/summary). Hash chống brute-force, TTL quyền mở, cooldown sau 5 lần sai. Executor/Beneficiary có quyền riêng, không dùng PIN Owner. Chủ sở hữu: Frontend Will + Backend Will.
2. **Beneficiary đã/chưa có tài khoản, email invitation, claim**: Cần hiển thị trạng thái từ `BeneficiaryAccountId`, email được xác minh; token một lần (lưu hash), gắn `WillId+BeneficiaryDesignationId`, chỉ release sau hồ sơ được duyệt, không gửi secret email. Chủ sở hữu: Will, Identity, Executor/Handover.
3. **Phí Executor 30/70**: Main Wallet hiện giữ phí toàn bộ và trả 100% khi xác nhận handover. Muốn 30% khi hoàn thành milestone mở hồ sơ **sau điều kiện pháp lý**, 70% sau công việc cuối cần schema PaymentMilestones/ledger idempotent, duyệt độc lập, dispute/refund. Main đã có `ReassignExecutorAsync` và PaymentDisputes; **không sửa WalletService/HandoverService trực tiếp trong commit Asset**. Nếu Executor chết trước Owner, không trả tiền vì chưa có mốc công việc; Executor thay thế theo quy trình hiện có. Chủ sở hữu: Backend Will/Wallet/Handover.
4. **Executor/Beneficiary thấy bản cập nhật mới**: hiện AssetId vẫn giữ nên nội dung lấy mới nhất tại thời điểm cấp quyền; Executor UI có thể hiển thị `UpdatedAt`, không thấy password. Cần audit/version cấp quyền; phần hiển thị ở Will/Executor do thành viên phụ trách.

## CÁCH GHÉP VÀO BRANCH HIỆN TẠI
**Ưu tiên dùng patch thay vì thay toàn bộ source**. Gói ZIP patch chứa các file theo đúng cấu trúc `LegacyVault_Final_Integrated/` + `MAIN_TO_ASSET_ONLY.patch`.

Tạo branch mới trước khi sửa: `git switch -c feature/asset-oauth-main`.
Nếu working tree sạch và trùng Main zip: `git apply --check MAIN_TO_ASSET_ONLY.patch` rồi `git apply MAIN_TO_ASSET_ONLY.patch`.
Nếu đã có thay đổi khác thì không apply mù; kiểm tra diff theo file, cherry-pick thủ công.

Kiểm tra: `git diff --stat`, `git status --short`, `git diff --check`.
Stage đúng file Asset và 2 file giao thoa (Program.cs, vaultApi.js) **sau khi team review**, không dùng `git add .`.
