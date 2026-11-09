# Owner FE — contract cần chốt với Nhiên (BE2) và Cường (BE3)

## Nhiên (N1–N6)
| Việc | FE đang giả định |
|---|---|
| Tài khoản bị khóa (N1/N2/N3) | Mọi API trả 401/403 kèm `{ "code": "ACCOUNT_LOCKED", "message": "..." }` → FE xóa token, về /login, hiện banner. Login cũng trả code này. |
| Rate limit (N4) | HTTP 429 + header `Retry-After` (giây) hoặc `retryAfterSeconds` trong body. Login sai mật khẩu có thể kèm `remainingAttempts`. |
| Refresh (N5) | `POST /auth/login` trả thêm `refreshToken`. `POST /auth/refresh {refreshToken}` → `{token, refreshToken}` (rotation, token cũ chết). `POST /auth/logout {refreshToken}` revoke. JWT hết hạn/bị revoke → 401 (KHÔNG có code ACCOUNT_LOCKED). |
| IdP (N6) | `POST /accounts/link-idp` trả 409, `code: "IDP_ALREADY_LINKED"` khi identity đã gắn tài khoản khác. |
| `/accounts/me` | nên có `accountStatus` (ACTIVE/LOCKED). |

Lưu ý: FE đang dùng 423 cho "PIN hết hạn" nên **đừng dùng 423 cho account lock**.

## Cường (C2–C6, C9)
| Việc | FE đang giả định |
|---|---|
| I'm alive (C6/C9) | `POST /handover/{willId}/alive` (Owner), 200/204. Sửa 1 dòng `OWNER_ALIVE_PATH` trong `vaultApi.js` nếu khác. Lỗi 409 nếu không còn hủy được. |
| Báo cáo niêm phong (C2) | `GET /handover/{willId}` thêm `report: {sha256, generatedAt}`; `GET /handover/{willId}/report` trả PDF. |
| Chữ ký (C3/C4) | `GET /handover/{willId}` thêm `signatures: [{role: EXECUTOR\|BENEFICIARY, signerName, signedAt}]`; item status thêm `AWAITING_EXECUTOR_SIGNATURE`, `AWAITING_BENEFICIARY_SIGNATURE`. |
