# Bang/model Cuong them o Sprint 3-4 va ly do

Tat ca deu la bang MOI (khong sua bang cua nguoi khac ngoai cac cot da liet ke trong notes).
Moi bang gan voi task cu the; khong co bang du phong.

| Bang | Task | Vi sao can |
|---|---|---|
| HandoverCases, HandoverItems | H01-H06 | Mot lan ban giao + tung tai san/nguoi nhan da giao chua (giao tung phan, pause/reopen) |
| LegalCases | E-legal / H02 | Ho so giay chung tu + ket qua Legal Verify, la dieu kien bat dau handover sau khi mat |
| Wallets, WalletTransactions | F01-F02 | So du va **ledger bat bien** (HOLD/RELEASE/REFUND/TOPUP, idempotency) |
| ServicePayments | F03-F05 | Phi dang giu cho tung Will (1 dong/Will), de release dung 1 lan |
| PaymentDisputes | F06 | Tranh chap phi (1 dong/Will) |
| ExecutorReviews | E09 / F07 | Danh gia Executor sau HANDED_OVER |
| AuditLogs | AD09 | Nhat ky "ai lam gi" cho Admin |

Cot them vao bang co san: ExecutorProfiles (phi, tien te, ho so, suspend), ExecutorAssignments (snapshot phi, scope),
AssetAssignments (AllocationPercentage), DmsConfigurations (GraceDays, moc nhac/nguong).

Neu nhom muon giam so bang, ung vien duy nhat khong mat tinh nang la gop PaymentDisputes vao ServicePayments
(da la quan he 1:1 theo Will). Cac bang con lai khong gop duoc ma khong mat yeu cau cua task.
