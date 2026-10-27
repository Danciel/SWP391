# Hợp nhất nhánh main vào sườn của mình

Nền: LegacyVault_Final_Integrated (bản của mình: Asset OAuth, Document, PIN 6 số).
Đã lấy từ main (SWP391-main1.zip) các phần dưới đây.

## Lấy từ main
Forgot / Reset password
- backend: AuthController, AuthService, IAuthService, DTO ForgotPasswordRequest + ResetPasswordRequest
- frontend: api/resetPasswordApi.js (mới), api/authApi.js, context/AuthContext.jsx

Executor Deposit (ứng trước 20%) + Executor CSV
- Models: ExecutorDeposit, ExecutorCsvSubmission, ExecutorProfile (TotalHeldDeposit)
- WalletService, IWalletService, WalletDtos (ExecutorDepositHeld)
- ExecutorInboxController (Accept -> cộng 20% rồi set ACTIVE)
- ExecutorCsvController, AdminExecutorCsvController
- LegacyVaultDbContext (2 DbSet + mapping + TotalHeldDeposit)
- database/s6_executor_deposit_and_csv.sql  <-- PHẢI CHẠY SQL NÀY trên DB

Audit Management
- Toàn bộ Features/AuditManagement (api/admin/audit-logs)

Frontend khác
- pages/Wallet/Wallet.jsx, pages/Layout/AppLayout.jsx (Executor có menu Wallet)
- pages/IdpRegister, IdpProfile, IdpProfileLookup, IdpFaceEnroll (sửa route /idp/...)
- pages/VerifyIdentity/VerifyIdentity.jsx (thông báo lỗi khuôn mặt của main)
- api/ownerApi.js (error có status + data)

## Giữ nguyên của mình
Program.cs, WillsController (PIN), AssetsController, toàn bộ AssetManagement,
Shared/Security (PIN), SecurityPinModal, securityPinApi, assetConnectionApi,
vaultApi.js, Assets.jsx, WillDetails.jsx, assetCatalog.js,
vite.config.js + launchSettings.json (cổng 7015).

## Sửa thêm (không có ở cả 2 bản)
- Program.cs: thêm đăng ký DI IAuditRepository / IAuditService.
  Main quên đăng ký nên AuditController sẽ lỗi khi gọi.

## Không đưa vào
bin/, obj/, .vs/, frontend.zip, thư mục lồng backend/LegacyVault/frontend/,
MockIdpApi.csproj.user, file rác x.AccountId / x.CreatedAt), frontend/.vite.

## Kiểm tra đã làm / chưa làm
- frontend: npm ci + npm run build thành công.
- backend: CHƯA build (môi trường không có .NET SDK). Cần chạy
  `dotnet build backend/LegacyVault.slnx` trên máy bạn.

## Thêm sau: trang Security (/dashboard/security)
Menu Security trước đây trỏ trùng /dashboard/profile, giờ có trang riêng.
- Password: Change password (cần mật khẩu hiện tại) + Forgot password (OTP email, dùng lại
  endpoint forgot-password / reset-password của main).
- Security PIN: Create (nếu chưa có), Change PIN (cần PIN hiện tại), Forgot PIN (OTP email).
- Chỉ hiện mục PIN với role OWNER/USER.

Backend mới:
- POST /api/auth/change-password           (AuthController, AuthService, ChangePasswordRequest)
- POST /api/owner-pin/change               (OwnerSecurityPinController)
- POST /api/owner-pin/forgot               (gửi OTP, cooldown 60s)
- POST /api/owner-pin/reset                (OTP + PIN mới, tối đa 5 lần sai OTP / 15 phút)
Frontend mới: pages/Security/*, securityPinApi.js (changePin, requestPinReset, resetPin),
authApi.js (changePassword), route "security" trong App.jsx, AppLayout.jsx (link Security).
Không đổi schema DB.
