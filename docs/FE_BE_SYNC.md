# FE ↔ BE sync guide

Frontend mới được đồng bộ vào repo này từ bản FE mới nhất.

## Cách FE biết gọi BE nào

`frontend/.env`:

```env
VITE_API_URL=https://localhost:7202/api
VITE_MOCK_MODE=true
```

`frontend/src/api/client.js` đọc hai biến này. Khi một module Backend hoàn thành, FE API wrapper của module đó chuyển từ mock sang `request(...)` thật.

## Kiểm tra một module đã nối đúng BE chưa

1. Xem component FE import API function từ file nào.
2. Xem API wrapper gọi path nào, ví dụ `/auth/login`.
3. Backend phải có route tương ứng, ví dụ `POST /api/auth/login`.
4. Chạy FE và BE, xem Network tab của browser để xác nhận status 2xx.

## Không liên quan tên project Supabase

Tên hiển thị Supabase project có thể khác tên FE/BE. BE kết nối database bằng `DefaultConnection` trong User Secrets. FE không cần biết tên database hoặc password Supabase.
