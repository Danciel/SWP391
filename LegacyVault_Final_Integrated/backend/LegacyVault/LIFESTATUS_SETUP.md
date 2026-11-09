# LifeStatus tu MockIdpApi (D05 / R10)

MockIdpApi la mot app rieng (thu muc ngang hang voi LegacyVault), mac dinh http://localhost:5100.
LegacyVault goi no qua HTTP (`IdpLifeStatusClient`):

1. `GET /api/idp-users/search?email=<email cua Owner>` -> `{ id, ... }`
2. `GET /api/idp-users/{id}` -> `{ ..., lifeStatus }`

Doi dia chi: them vao appsettings (hoac user-secrets) `"Idp": { "BaseUrl": "http://localhost:5100" }`.

## Quy tac an toan
Khong co email, IdP khong tra loi/cham (> 5 giay), 404, nhieu tai khoan, hoac gia tri la
(khac ALIVE/UNKNOWN/PENDING/DECEASED) -> tra `UNKNOWN`. Khong bao gio tu suy ra DECEASED.

## Chay thu
1. Chay MockIdpApi (`dotnet run` trong thu muc MockIdpApi, cong 5100) va LegacyVault.
2. Email Account cua Owner trong LegacyVault phai **trung** email IdpUser trong MockIdp
   (tao bang `POST /api/idp-users` neu chua co).
3. Chuyen Owner thanh da mat: `PUT /api/idp-users/{id}` voi `{ "lifeStatus": "DECEASED" }`.
4. Legal case detail hien `lifeStatus = DECEASED`; Executor `POST /api/handover/{willId}/start` chay duoc.
5. Dat lai `ALIVE` -> `start` tra 409 "LifeStatus is ALIVE". Tat MockIdp -> `UNKNOWN` (409), khong loi 500.

Luu y: `MockIdpApi/appsettings.Development.json` dang chua chuoi ket noi Supabase kem mat khau;
nen chuyen sang user-secrets va doi mat khau neu file da duoc commit.
