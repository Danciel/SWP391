# S3 - phan con lai cua Cuong (theo LegacyVault_Tasks.xlsx)

Nen: code zip W06_W08-changes (LV-13..15, LV-17..19, LV-21 da co trong do) + S1/S2.

## Migration (leader cho phep)
    dotnet ef migrations add AddHandoverLegalWalletReviews
    dotnet ef database update
Neu da chay `AddW06AllocationAndExecutorFeeSnapshot` + s3_w06_w08_fee_columns.sql roi thi chi can lenh tren.
Cot/bang moi: BeneficiaryDesignations.IdentityNumber, ExecutorProfiles.Specialty, ExecutorReviews, LegalCases,
HandoverCases, HandoverItems, Wallets, WalletTransactions, ServicePayments.
Cau hinh them (tuy chon): Storage:Root (thu muc luu giay chung tu, mac dinh App_Data), App:FrontendUrl (link trong email moi).

## Task -> API
| Task | API |
|---|---|
| LV-15 | POST /api/wills/{id}/beneficiaries them `identityNumber` (response chi tra 3 ky tu cuoi) |
| LV-20 | PUT /api/executors/me/profile; GET /api/executors/{accountId}; GET /api/executors (co specialty) |
| LV-21 | PUT /api/executors/me/pricing (zip); phi chot luc chon Executor = ServiceFeeSnapshot |
| LV-22 | GET/POST /api/executors/{accountId}/reviews (chi Owner, Will da HANDED_OVER, 1 review/Will) |
| LV-24/26 | Email nhac (D03); GET /api/dms/alerts cho FE hien "Miss 1/3" |
| LV-29 | POST /api/legal-cases (multipart: willId, file) - Executor ACCEPTED, DMS THRESHOLD_REACHED |
| LV-30 | GET /api/legal-cases, GET {id}, GET {id}/certificate, POST {id}/decision (chi LEGAL_VERIFIER) |
| LV-16 | Khi handover bat dau, email moi nguoi nhan chua co tai khoan (dang ky bang cung email) |
| LV-32 | POST /api/handover/{willId}/immediate (Owner, Will ACTIVE) |
| LV-33 | POST /api/handover/{willId}/start (Executor; can legal APPROVED + LifeStatus DECEASED); GET /api/handover/{willId}; POST /api/handover/items/{id}/deliver |
| LV-34 | GET /api/wallet; POST /api/wallet/topup (mock) |
| LV-35 | Submit giu phi (HOLD); Decline/Cancel hoan; giao het item -> RELEASE cho Executor; POST /api/handover/{willId}/abort (ADMIN) hoan phi |
| LV-38 | GET /api/admin/dms; POST /api/admin/dms/{willId}/configure; POST /api/admin/dms/evaluate {now} (chi ADMIN) |
| LV-31 | DMS_COST_ANALYSIS.md |

## Chua lam / phu thuoc nguoi khac
- LV-19: theo quyet dinh cua Cuong, Executor phu trach THEO WILL (khong theo asset). Bang yeu cau goc ghi "theo Asset/Beneficiary" -> can bao co/leader.
- LV-28: LifeStatus that cua Nhien. Hien NullLifeStatusClient tra UNKNOWN => luong "start after death" bi chan cho den khi thay class. Immediate khong bi anh huong.
- LV-16: token moi dung 1 lan (I08) la cua Nhien; o day chi gui email moi chung + lien ket bang email xac thuc (POST .../beneficiaries/{id}/link).
- LV-36/39: Admin chi co DMS demo; role LEGAL_VERIFIER/ADMIN do Nhien cap.
- LV-03: minh chung API/DB truoc-sau: xem LegacyVault.http (them cac request moi khi test).

## Quy uoc can bao Kiet
Module moi (wallet, legal, handover, review, admin DMS) dung DbContext truc tiep trong Service, KHONG co Repository rieng
(giong ExecutorInboxController co san). Neu can dung Controller->Service->Repository thi tach tiep.
Program.cs: them 4 dong AddScoped. Data/LegacyVaultDbContext.cs: them DbSet + cau hinh.
