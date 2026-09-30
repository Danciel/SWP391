# LegacyVault Integrated v2

Bản này đồng bộ **frontend mới nhất** với backend ASP.NET Core hiện có, đồng thời tạo sẵn **package skeleton cho các Backend**. Skeleton chỉ chia folder/phạm vi; không viết nghiệp vụ thay thành viên.

## Cấu trúc

```text
LegacyVault_Integrated_v2/
├── frontend/                    # FE mới nhất React/Vite
├── backend/
│   ├── SWP301BETASK.slnx
│   └── SWP301BETASK/
│       ├── Controllers/         # code cũ đang chạy
│       ├── Data/
│       ├── Models/
│       ├── Migrations/
│       ├── Features/            # package skeleton mới
│       └── Shared/
├── database/
│   └── mock_external_accounts.sql
├── docs/
│   ├── BACKEND_PACKAGE_MAP.md
│   └── FE_BE_SYNC.md
├── run-backend.bat
└── run-frontend.bat
```

## Phân BE

- **Nhiên — IdentityAccess**: Account/Auth/OTP/JWT/Profile/Identity Verification.
- **Kiệt — AssetManagement**: Asset CRUD + mock asset verification + integration.
- **Cường — WillManagement**: Will + Beneficiary + Assignment + Executor + DMS.
- `LegalVerification` và `Admin` được tạo package placeholder, chưa ép owner.

Xem sơ đồ đầy đủ tại `docs/BACKEND_PACKAGE_MAP.md`.

## Chạy Backend

1. Mở `backend/SWP301BETASK.slnx`.
2. `Manage User Secrets`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;"
  }
}
```

3. Run profile `https`, hoặc chạy `run-backend.bat`.

Mặc định:

```text
https://localhost:7202
```

Test:

```text
GET https://localhost:7202/api/health
GET https://localhost:7202/api/accounts
```

## Chạy Frontend

```powershell
cd frontend
copy .env.example .env
npm install
npm run dev
```

Mặc định:

```text
http://localhost:5173
```

`.env.example`:

```env
VITE_API_URL=https://localhost:7202/api
VITE_MOCK_MODE=true
```

## Database mock provider

`database/mock_external_accounts.sql` dùng để tạo dữ liệu Gmail/GitHub giả trên Supabase cho Asset Verification demo.

## Git

Không commit `.env`, User Secrets, database password, `node_modules`, `bin`, `obj`, `.vs`.
