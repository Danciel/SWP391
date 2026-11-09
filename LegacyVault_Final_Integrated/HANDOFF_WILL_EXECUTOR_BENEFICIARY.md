# Handoff: các nghiệp vụ chung chưa nên commit cùng Asset

Tài liệu này dành cho Owner module Will/Executor/Wallet/Identity. Không mô tả tính năng đã hoạt động; đây là yêu cầu để code-review.

## A. Executor Payment — 30% / 70%
- Khi Owner lập Will và chọn Executor: thỏa thuận tổng phí, giữ ngân sách hoặc tiền mô phỏng ở trạng thái HELD, chưa chuyển cho Executor.
- Executor nhấn ACCEPT: ghi nhận vai trò, **không trả 30%**.
- Chỉ khi có hồ sơ thực thi đã đủ điều kiện (không chỉ DMS missed check-in), Executor hoàn tất milestone mở hồ sơ và có xác nhận độc lập thì trả **30%**.
- Khi hoàn tất tất cả assignment và điều kiện bàn giao đã được xác nhận, trả **70%** còn lại. Mỗi milestone trả tiền idempotent, có transaction/audit, chống self-approval và double release.
- Nếu Executor mất khả năng, qua đời hoặc Owner sống lâu hơn Executor trước khi thực thi: không có tiền được release, chỉ reassign Executor. Nếu đã trả 30%, giữ 70% và xét nghĩa vụ hoàn thành/tranh chấp/đền bù theo thỏa thuận.
- Main đã có `WalletService.HoldFeeAsync`, `ReleaseFeeAsync`, `RefundFeeAsync`, `HandoverService.ReassignExecutorAsync`, `PaymentDisputes`: cần nâng cấp state và ledger. KHÔNG sửa các class này trong PR Asset.

## B. Will Detail PIN 6 số
- Owner thiết lập PIN độc lập với mật khẩu login; bắt buộc khi mở chi tiết Will đã Submit/ACTIVE. DRAFT không thay đổi luồng hiện tại nếu nhóm đồng ý.
- Backend lưu hash PIN, secret server-side pepper, giới hạn lần sai, khóa tạm thời, session unlock gắn owner/will/device có TTL ngắn; rate-limit theo account+IP; reset qua reauthentication.
- Đừng chỉ ẩn giao diện: GET /api/wills/{id} và endpoint trả chi tiết tương tự phải kiểm soát quyền; list/dashboard trả summary không chứa secret.
- Executor/Beneficiary không cần biết PIN Owner.

## C. Beneficiary invitation / claim
- Ở CreateWill / WillDetails / Executor / MyInheritance: phân biệt `REGISTERED_VERIFIED`, `REGISTERED_UNVERIFIED`, `NOT_REGISTERED`, `CLAIMED` từ Account+BeneficiaryDesignation.
- Sau phê duyệt handover, gửi email đến địa chỉ Owner chỉ định. Link chứa opaque invitation token một lần, lưu hash ở DB; token gắn willId + beneficiaryDesignationId + email + expiry + trạng thái.
- Nếu chưa đăng ký: người nhận đăng ký bằng đúng email; xác minh email OTP; sau đó xác minh danh tính theo quy trình có thẩm quyền và dùng token để claim.
- Backend kiểm tra link, account email đã verify, Claim/Beneficiary/Will/Assignment đúng, giao dịch atomic cập nhật BeneficiaryAccountId và audit. Không gửi secret qua email.
- Chỉ release từng Asset được gắn BeneficiaryDesignation trong Will sau khi Case Approved; tài sản đọc latest Secret có kiểm tra version/release audit.

## D. Quy ước phân chia PR
- PR Asset: đúng 11 file trong manifest của bản merge.
- PR PIN: Will/Identity, migrations, API + UI; không trộn branch Asset.
- PR Beneficiary Invitation: Will/Identity/Notifications; contract API thống nhất.
- PR PaymentMilestones: Wallet/Handover/Executor; migration + idempotency + dispute tests.
