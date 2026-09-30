# Frontend integration notes

Bản frontend này đã được đồng bộ vào LegacyVault Integrated v2.

- API base URL lấy từ `VITE_API_URL`.
- Mock mode lấy từ `VITE_MOCK_MODE`.
- Mặc định `VITE_MOCK_MODE=true` để UI mới vẫn chạy trong khi BE đang được chia package và implement dần.
- Không đổi toàn bộ sang `false` cho tới khi các endpoint FE đang gọi đã tồn tại ở BE.

Cách kiểm tra:

```text
FE function -> request('/path') -> https://localhost:7202/api/path -> BE controller route
```
