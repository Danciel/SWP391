# S1 - Cuong (W01, W02, W04, W05) - tren code SWP391

Chi sua trong Features/WillManagement. KHONG doi schema/migration/Program.cs.

- W01: POST /api/wills/{id}/cancel (DRAFT hoac PENDING_EXECUTOR -> CANCELLED; executor PENDING -> CANCELLED; DMS -> CANCELLED). Trang thai khac -> 409.
- W02: AddAsset khoa doc quyen: pg_advisory_xact_lock theo AssetId trong transaction + kiem tra Will khac chua CANCELLED. Trung -> 409. GET /api/wills/locked-assets tra danh sach AssetId dang bi khoa (cho A06).
  R01 (DRAFT co lock ngay?) dang gia dinh CO. Doi tai NonLockingStatuses trong WillRepository.
- W04: Relationship bat buoc khi them beneficiary; assignments trong detail tra them BeneficiaryName + Relationship.
- W05: Nguoi chua co Account luu rieng (FullName, Email, Phone). POST /api/wills/{id}/beneficiaries/{bid}/link {accountId}: chi lien ket khi Account.EmailVerified va Email trung. Tu dong lien ket khi dang ky = viec cua Nhien (I08).

Chua chay dotnet build (sandbox khong co .NET SDK). Ban chay: dotnet build.

# S2 (dot 1) - W03, D02, D04, E03
- W03: DELETE /api/wills/{id}/assets/{willAssetId} va DELETE /api/wills/{id}/assignments/{assignmentId} (chi DRAFT). Xoa WillAsset = nha khoa W02.
- D02: check-in cho phep khi DMS ACTIVE hoac THRESHOLD_REACHED; reset MissedCheckIns, Status=ACTIVE, NextCheckInAt moi.
- D04: DmsCycleCalculator + DmsMissedCycleWorker (moi gio). Moi chu ky chi ghi 1 dong DmsCheckIns MISSED, NextCheckInAt tien len => chay lap khong dem trung; bu duoc luc server tat. Grace mac dinh 2 ngay (R03 chua chot). Dat nguong -> Status=THRESHOLD_REACHED, KHONG danh dau chet/ban giao (R04).
- E03: Respond dung UPDATE co dieu kien Status=PENDING; tra loi lan 2 -> 409.
- Program.cs: them 1 dong AddHostedService (file shared, bao Kiet).

# S2 (dot 2) - D01, D03, D05, D06 (can migration)
Cot moi DmsConfigurations: GraceDays(def 2), LastReminderForDueAt, ThresholdReachedAt, ExecutorNotifiedAt.
Chay: dotnet ef migrations add AddDmsGraceAndNotifyColumns  (hoac database/s2_dms_columns.sql), roi database update.
- D01: ConfigureDms nhan/luu GraceDays (0-30).
- D03: IEmailSender.SendDmsReminderEmailAsync; worker nhac Owner trong cua so ReminderBeforeDays hoac khi vua miss; LastReminderForDueAt chong nhac trung; mail loi thi thu lai gio sau.
- D05: dat nguong -> THRESHOLD_REACHED + ThresholdReachedAt; ILifeStatusClient (tam tra UNKNOWN) chi log; khong danh dau chet/ban giao.
- D06: chi gui mail cho Executor da ACCEPTED, 1 lan (ExecutorNotifiedAt); GET /api/executor-inbox/triggers.
- IEmailSender doi: file cua Nhien/Kiet (them 2 ham, tach SendAsync dung chung) - bao ho.
