# LegacyVault v8 — Google Picker & Executor suggestions (bản chỉnh sửa)

## 1. Những thay đổi đã thực hiện

- **Cloud Storage / Google Drive** trong **Assets → Add Asset** và **Create Will → Add New Asset**: không nhập File ID thủ công. Chọn **Select Drive File & Add** → OAuth popup → Google Picker → chọn file → backend gọi Google Drive `files.get` → tạo Asset `FILE_ACCESS_VERIFIED`.
- Backend dùng OAuth state + ticket hiện có, **chỉ cấp tạm Google access token cho Picker** khi JWT Owner trùng ticket. Không lưu access token / refresh token vào DB, `localStorage` hoặc AssetSecret. Ticket 10 phút và một lần sử dụng.
- Google Picker API key và Project Number được lấy từ User Secrets của backend, không hardcode trong frontend. **API key vẫn hiện cho trình duyệt**, nên phải giới hạn theo HTTP referrers và API.
- Gỡ các **gợi ý Executor** trên UI: gợi ý chuyên gia theo tài sản trong Create Will/Will Details, gợi ý mức phí và nút áp dụng giá gợi ý trong Owner/Executor/Admin. **Giữ chọn Main Executor, xem đánh giá, sửa phí do Executor tự nhập, luồng Will/Handover và các API backend cũ**. Các bản ghi chuyên gia cũ không bị tự xóa.

## 2. Bước cấu hình Google Cloud (Project LegacyVault)

1. Mở https://console.cloud.google.com/ → chọn đúng Project LegacyVault.
2. **APIs & Services → Library**: đảm bảo **Google Drive API** và **Google Picker API** đều được `Enabled`.
3. **Google Auth Platform → Audience**: nếu `External / Testing`, thêm Gmail dùng để test vào **Test users**.
4. **Google Auth Platform → Data Access**: scope `openid`, `email`, `https://www.googleapis.com/auth/drive.file` (theo OAuth flow v8).
5. **Google Auth Platform → Clients**: dùng Client `LegacyVault Local` hiện có. Kiểm tra Authorized Redirect URI:

   `https://localhost:7015/api/assets/connections/google_drive/callback`

   `http://localhost:5173` là JavaScript origin phù hợp cho frontend localhost.
6. **APIs & Services → Credentials → Create credentials → API key**: tạo API key cho Google Picker.
   - Application restrictions: **Websites** / HTTP referrers; thêm **`http://localhost:5173/*`** và **`https://docs.google.com/*`**. Google Picker chạy trong iframe `docs.google.com`, nếu bỏ qua URL này có thể báo `API developer key is invalid`.
   - API restrictions: chọn **Google Picker API**; chỉ bổ sung Drive API nếu sau này frontend gọi trực tiếp Drive bằng key này.
7. **Cloud project number**: trên Cloud Console, mở Project information / Dashboard để lấy **Project number** (chuỗi toàn chữ số), **KHÔNG** phải Project ID (`legacyvault324`) hay OAuth Client ID.

## 3. Cấu hình trong Visual Studio → Manage User Secrets

Chuột phải **project** `LegacyVault` (không phải Solution) → **Manage User Secrets**, thêm hoặc gộp các key này với cấu hình có sẵn. Đừng gửi hoặc commit các giá trị thật.

```json
{
  "OAuth": {
    "GOOGLE_DRIVE": {
      "ClientId": "YOUR_EXISTING_OAUTH_CLIENT_ID",
      "ClientSecret": "YOUR_EXISTING_OAUTH_CLIENT_SECRET"
    },
    "CallbackBaseUrl": "https://localhost:7015"
  },
  "GooglePicker": {
    "ApiKey": "YOUR_GOOGLE_PICKER_API_KEY",
    "AppId": "YOUR_NUMERIC_GOOGLE_CLOUD_PROJECT_NUMBER"
  }
}
```

**Nếu đã có `OAuth`, hãy thêm hai key `GooglePicker` và giữ các giá trị cũ.** Không cần tạo lại Client ID/Client Secret nếu chúng đang hoạt động. Không thay đổi KEK hiện có.

Bạn cũng có thể cấu hình hai key mới bằng PowerShell trong `backend/LegacyVault`:

```powershell
dotnet user-secrets set "GooglePicker:ApiKey" "API_KEY_CUA_BAN"
dotnet user-secrets set "GooglePicker:AppId" "PROJECT_NUMBER_CUA_BAN"
```

## 4. Chạy và test

- Backend trong `backend/LegacyVault`: `dotnet run --launch-profile https` (HTTPS `7015`, PostgreSQL và config khác phải hoạt động).
- Frontend trong `frontend`: `npm install` rồi `npm run dev` (`http://localhost:5173`).
- Đăng nhập Owner → **Assets → Add Asset** → Asset Type `Cloud Storage` → đặt Asset Name → nhấn **Select Drive File & Add**.
- Đăng nhập Google trong popup; sau khi popup đóng, Google Picker sẽ mở trong trang → chọn một file → xác nhận.
- Kỳ vọng `FILE_ACCESS_VERIFIED`; file ID được kiểm tra trên **backend**, không tin ID do browser gửi.
- Kiểm tra tương tự ở **Create Will → Assets → Add New Asset**.
- Executor: ở **Create Will → Executor**, vẫn phải chọn được Main Executor và xem Review; không còn mục Specialist gợi ý hay Suggested Fee. Executor Dashboard vẫn sửa Fee thủ công được.

## 5. Lỗi thường gặp

| Triệu chứng | Kiểm tra |
| --- | --- |
| `GooglePicker:ApiKey and GooglePicker:AppId` | Chưa thêm 2 key mới vào User Secrets / chưa restart backend |
| `API developer key is invalid` | API key, Google Picker API, referrer gồm `docs.google.com/*` và `localhost:5173/*` |
| `redirect_uri_mismatch` | Callback `https://localhost:7015/api/assets/connections/google_drive/callback` không khớp Google Cloud |
| Google 404 sau khi Picker chọn | Xác nhận Project number, Google Picker API enabled, file không bị xóa / mất quyền; OAuth token và Picker thuộc cùng Cloud project |
| `Google Drive selection expired` | Mở lại Add Asset và chạy OAuth + Picker lại trong vòng 10 phút |
| Vite không thấy form sửa | Đảm bảo backend & frontend đều khởi động lại đúng source bản đã chỉnh |

## 6. Ghi chú phạm vi

- **Đã kiểm tra cú pháp JSX/JS**. Môi trường tạo bản chỉnh không có .NET SDK để build/kiểm thử C# và không thể xác thực OAuth Google thật thay tài khoản của bạn; cần chạy smoke test như mục 4 trên máy.
- Chỉ xác minh quyền truy cập file tại thời điểm thêm Asset; **chưa tải và mã hóa bản sao file**, chưa bảo đảm quyền truy cập cho Beneficiary sau khi Owner qua đời.
- Dữ liệu cũ và bảng DB giữ nguyên, không tạo migration.

Tài liệu tham khảo: https://developers.google.com/workspace/drive/picker/guides/web-picker
