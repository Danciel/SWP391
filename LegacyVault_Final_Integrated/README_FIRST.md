# LegacyVault Final Integrated - Backend + Frontend Nhật

Bản này ghép:

- Backend hiện tại đã build thành công trên máy Kiệt: Account/Auth/Gmail OTP/JWT/Search.
- Task Kiệt: Asset CRUD + AES-256-GCM envelope encryption + mock provider verification.
- Task Cường: Will + Beneficiary + Assignment + Executor + DMS.
- Frontend Nhật: giữ giao diện, đổi core flow sang backend thật.

## Core flow đang dùng backend thật

- Register
- Gmail OTP verify / resend
- Login + JWT
- Profile (`GET /api/accounts/me`)
- Beneficiary search bằng 1 input Email hoặc Phone
- Asset CRUD
- Asset Reveal Secret / Verify
- Will Draft / Assets / Beneficiary / Assignment / Executor / DMS / Submit
- My Wills / Will Details / DMS Check-in

## Hybrid/mock vẫn giữ

Theo scope hiện tại, các phần nâng cao chưa có backend đầy đủ vẫn mock:

- Phone OTP thật
- Face / liveness
- Third-party identity provider thật
- Inherited-will delivery nâng cao
- Executor operational dashboard nâng cao
- Legal verifier workflow nâng cao
- Admin workflow nâng cao

`VITE_MOCK_MODE=false` nhưng `VITE_ADVANCED_MOCK_MODE=true`.

## Chạy project

### Backend

Mở:

`backend/LegacyVault.slnx`

Đảm bảo User Secrets hiện tại vẫn có:

- ConnectionStrings:DefaultConnection
- Email settings
- Jwt settings
- Encryption:KekBase64
- Encryption:KekVersion = 1

Sau đó chạy profile HTTPS. Backend mặc định:

`https://localhost:7015`

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

Frontend:

`http://localhost:5173`

`.env` đã trỏ đến:

`https://localhost:7015/api`

Backend đã được thêm CORS cho Vite port 5173.

## Lưu ý

Không commit User Secrets. Không chụp màn hình secrets.json có giá trị thật.
