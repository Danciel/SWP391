# Test LegacyVault trực tiếp bằng Frontend

## 0. Trước khi test

Backend phải chạy `https://localhost:7015` và frontend chạy `http://localhost:5173`.

Nếu Asset create báo thiếu KEK, cấu hình `Encryption:KekBase64` trong User Secrets trước.

Nếu Online Account Verify không tìm thấy demo provider, chạy file:

`backend/LegacyVault/database/seed_mock_external_accounts_if_needed.sql`

trong Supabase SQL Editor.

## 1. Login

Có thể dùng account demo đã seed:

- Email: `kiet.user@legacyvault.demo`
- Password: `Demo@123`

Hoặc test Register -> Gmail OTP -> Login bằng account mới.

Sau login role backend là `USER`; frontend đã map `USER` về giao diện Assets/Create Will/My Wills.

## 2. Test Asset + Encryption

Vào `Assets` -> `Add Asset`.

Test dễ nhất:

- Asset Type: Online Account
- Provider: GMAIL
- Username: `demo.gmail@gmail.com`
- Password: `External@123`

Sau khi tạo:

1. Asset list phải hiện asset nhưng không hiện password.
2. Mở asset -> `Reveal Secret`: backend decrypt AES-256-GCM và trả secret.
3. Bấm `Verify`: expected `VERIFIED` nếu mock provider seed tồn tại.
4. Có thể Edit metadata / Delete.

## 3. Test Create Will

Vào `Create Will`:

1. Information: nhập tên Will -> Next.
2. Assets: chọn Asset vừa tạo.
3. Beneficiaries:
   - Chọn Existing Account.
   - Một ô duy nhất nhận Email hoặc Phone.
   - Email demo: `minhanh.user@legacyvault.demo`
   - Hoặc phone: `+84901000002`
   - Frontend tự URL-encode dấu `+`.
   - Search result chỉ hiện tên + email/phone đã mask + IdentityStatus.
4. Executor: chọn một executor verified từ backend.
5. DMS: chọn interval/reminder/max missed.
6. Review -> Submit Will.

Expected status sau submit:

`PENDING_EXECUTOR`

DMS expected:

`ACTIVE`

## 4. Test My Wills

Vào `My Wills`:

- Draft -> Continue Editing.
- Draft có thể Delete.
- Submitted Will -> View Details.
- View Details hiển thị Asset -> Beneficiary, Executor, DMS.
- Khi DMS ACTIVE có thể bấm `I'm Still Active / Check In`.

## 5. Nếu FE báo lỗi mạng

Kiểm tra:

- Backend có đang chạy HTTPS port 7015 không.
- Browser đã trust ASP.NET development HTTPS certificate chưa.
- Frontend đang chạy đúng port 5173 không.
- `.env` có `VITE_API_URL=https://localhost:7015/api` không.
- Sau khi sửa `.env`, restart `npm run dev`.
