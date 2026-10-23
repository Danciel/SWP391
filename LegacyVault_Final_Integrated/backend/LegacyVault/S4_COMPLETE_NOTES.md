# Sprint 4 - phan bo sung de hoan thien (Cuong)

Nen: W06_W08 + S3 + S4-changes. Dat de len `backend/LegacyVault`.
**Khong can migration moi**: khong them cot/bang/index. Trang thai moi chi la gia tri chuoi
(`HandoverCases.Status = PAUSED`, `PaymentDisputes.ResolutionAction = RESUME | REASSIGN`).

## Da lam them

| Task | Noi dung |
|---|---|
| H06 | Dispute -> **tu dong PAUSE** handover dang chay. Admin: `POST /api/handover/{willId}/pause`, `/resume`, `POST /api/handover/items/{id}/reopen` (revise), `POST /api/handover/{willId}/reassign-executor`. `POST /api/handover/{willId}/abort` nay xu ly ca case PAUSED. |
| F05/H06 | **Dispute dang OPEN chan giai ngan**: `ReleaseFeeAsync` khong giai ngan khi co dispute OPEN (kiem tra nam trong cau UPDATE nguyen tu, nen khong bi race). Chi `RELEASE` cua Admin moi bo qua. Ghi audit `PAYMENT_RELEASE_BLOCKED_BY_DISPUTE`. |
| F06 | `POST /api/admin/payment-disputes/{id}/resolve` nhan `REFUND`, `RELEASE`, `RESUME`, `REASSIGN` (+ `newExecutorAccountId`). Dispute da RESOLVED khong resolve lai (409). Mo dispute moi sau khi da giai quyet: dong cu duoc mo lai (giu 1 dong/Will), ket qua cu nam trong audit. |
| H06 | Khong cho **bat dau handover** khi con dispute OPEN. |
| Executor decline | Owner chon lai Executor khi Will dang `PENDING_EXECUTOR` va Executor cu da `DECLINED`; phi cua Executor moi duoc HOLD lai (thieu so du -> loi, khong doi gi). Khong chon lai chinh Executor da tu choi. |
| E10 | `GET /api/executors/pricing-reference`: min/median/average/max tu phi Executor da verify, VND, can >= 3 mau, kem trung binh phi da RELEASE va bang so sanh (% lech so voi median). Co `source` + `disclaimer` "khong phai gia thi truong". Khong du mau -> `hasReferenceData=false`, khong co thong ke. |
| W09 | `GET /api/relationships/me` (owner / beneficiary / executor) va `GET /api/relationships/assets/{assetId}` (chi chu asset). Nguoi nhan chi thay Will khi handover da bat dau (`HANDOVER_IN_PROGRESS`/`HANDED_OVER`) va **chi thay quan he + tai san cua chinh minh**. Executor khong thay du lieu nguoi nhan. |
| AD09 | Them audit cho: `WILL_CREATED/DELETED`, `WILL_ASSET_ADDED/REMOVED`, `BENEFICIARY_ADDED`, `DMS_CONFIGURED`, `DMS_CHECKED_IN`, `WILL_SUBMITTED`, `WILL_CANCELLED`, `EXECUTOR_SELECTED`, `EXECUTOR_ACCEPTED/DECLINED`, `HANDOVER_PAUSED/RESUMED/ITEM_REOPENED`, `EXECUTOR_REASSIGNED`, `PAYMENT_DISPUTE_REOPENED`. Chi luu metadata, khong luu bi mat. |
| Fix | `HoldFeeAsync` reload dong ServicePayment truoc khi kiem tra (tranh doc ban cu trong cung request khi re-hold). |

## Luong H06

    HANDOVER IN_PROGRESS --(dispute mo / admin pause)--> PAUSED
    PAUSED --RESUME--> IN_PROGRESS            (phi van HELD; giao tiep tuc)
    PAUSED --reopen item--> item DELIVERED -> PENDING (revise)
    PAUSED / Will ACTIVE --REASSIGN--> case ABORTED, hoan phi cu, HOLD phi Executor moi,
                                      Will PENDING_EXECUTOR, Executor moi accept -> ACTIVE
    PAUSED --REFUND--> abort + hoan phi          HANDED_OVER + HELD --RELEASE--> giai ngan

Quy uoc can chot voi nhom: sau REASSIGN, cac item da giao cua Executor cu van nam trong case ABORTED
(de doi chieu); Executor moi bat dau handover moi, tat ca item ve PENDING.

## Kiem thu de xuat
1. Executor dang giao do, Owner mo dispute -> `handoverPaused=true`; Executor `deliver` -> 409 "paused".
2. Dispute OPEN -> `resume` thu cong -> 409; resolve `RESUME` -> case IN_PROGRESS, phi HELD, giao tiep tuc.
3. Pause -> `reopen` mot item da DELIVERED -> PENDING; khi handover khong PAUSED -> 409.
4. Resolve `REASSIGN`: so du Owner co 1 dong REFUND + 1 dong HOLD; Will `PENDING_EXECUTOR`; Executor moi accept -> `ACTIVE`; decline -> hoan phi, Owner chon lai duoc.
5. Dispute OPEN khi Will ACTIVE -> `immediate`/`start` bi 409; resolve xong thi bat dau duoc.
6. Resolve `RELEASE` khi Will chua HANDED_OVER -> 409; khi HANDED_OVER + HELD -> RELEASED dung 1 lan.
7. Suspend Executor -> khong chon lai/reassign sang Executor bi suspend.
8. `GET /api/admin/audit-logs?entityType=Will` hien du hanh dong cua Owner.
9. `pricing-reference` voi < 3 Executor co phi -> `hasReferenceData=false`.
10. Nguoi nhan A goi `relationships/me` -> khong thay nguoi nhan B hay phan chia cua B.

## Chua kiem chung
Moi truong nay khong co .NET SDK nen **chua** `dotnet build`, chua chay test, chua thu voi DB.
Da kiem tra tinh (can bang ngoac, doi chieu ten ham/kieu). Hay `dotnet build` truoc khi merge va gui lai loi neu co.
Chua co thay doi FE: Nhat/Phuc can map cac endpoint moi (xem bang tren va `LegacyVault.http`).
