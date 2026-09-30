# Backend feature packages

Các folder ở đây là **package skeleton** để chia việc. Chưa có implementation C# bắt buộc; mỗi thành viên tự code trong package của mình.

- `IdentityAccess` — Nhiên: Account, Register/Login, OTP, JWT, Profile, Identity Verification.
- `AssetManagement` — Kiệt: Asset CRUD, mock asset verification, encryption integration sau này, database/integration review.
- `WillManagement` — Cường: Will, Beneficiary, Asset Assignment, Executor Assignment, DMS.
- `LegalVerification` — package để nối flow Legal Verifier khi nhóm bắt đầu module này; phân công chính thức có thể chốt sau.
- `Admin` — package để nối các API Admin; phân công chính thức có thể chốt sau.
- `Shared` — code dùng chung, tránh đưa nghiệp vụ riêng của một feature vào đây.

## Quy ước package bên trong một feature

```text
FeatureName/
├── Controllers/    # HTTP endpoints
├── DTOs/           # request/response contracts
├── Models/         # entity/domain model thuộc feature
├── Services/       # business logic
├── Repositories/   # data access nếu nhóm dùng repository pattern
└── Contracts/      # interfaces/contracts giữa các layer
```

Không bắt buộc phải dùng Repository nếu nhóm quyết định DbContext trực tiếp trong Service. Quan trọng là cả nhóm chọn một convention và dùng nhất quán.
