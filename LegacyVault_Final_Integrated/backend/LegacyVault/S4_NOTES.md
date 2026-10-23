# Sprint 4 stabilization additions (Cường)

## Added / completed in this patch

- **AD05 Executor oversight:** `GET /api/admin/executors`, `PUT /api/admin/executors/{accountId}/suspension`. Only ADMIN. Suspension is audited and blocks new selection, Executor acceptance, death-case submission, and Executor-driven handover start/item delivery. Existing history is not rewritten.
- **AD07 Legal oversight:** `GET /api/admin/legal-cases` is read-only. ADMIN cannot approve/reject; decisions remain `LEGAL_VERIFIER` only.
- **AD08 / F06 Payment management & disputes:** `GET /api/admin/payments` returns paged ServicePayments plus their wallet-ledger entries. `POST /api/wallet/disputes` and `GET /api/wallet/disputes/mine` let the Owner or assigned Executor raise/view a dispute while a fee is held. ADMIN uses `GET /api/admin/payment-disputes` and `POST /api/admin/payment-disputes/{id}/resolve` to resolve with a required note and `REFUND` or `RELEASE`; refund aborts an in-progress handover, release requires `HANDED_OVER`. Money is settled only through the idempotent WalletService.
- **AD09 Audit viewer:** `GET /api/admin/audit-logs` supports action, entityType, actorAccountId, from/to, page and pageSize filters. Wallet, legal, handover, review/profile and Admin DMS actions write audit rows.
- **F02 top-up idempotency:** `POST /api/wallet/topup` requires an `Idempotency-Key` header. Reusing the same key for the same account returns the current wallet without crediting again. Use a new key for each intentional top-up.
- Moved Wallet/Profile/Legal/Handover service registrations into the `CUONG - WILL FLOW` section in `Program.cs`.
- Added Sprint 4 requests to `LegacyVault.http`.

## Database update

Preferred when EF migrations are the project's chosen workflow:

1. If Sprint 3 migration has not been created yet, first run the existing Sprint 3 migration workflow documented in `S3_NOTES.md` (`AddHandoverLegalWalletReviews`).
2. Then run:
   `dotnet ef migrations add AddSprint4AdminStabilization`
   `dotnet ef database update`

Alternative for a database already containing the Sprint 3 tables: run `database/s4_admin_stabilization.sql` once. **Do not run both the generated migration and the SQL script for the same changes.**

## Suggested verification

- ADMIN can list executors and suspend/reinstate one; non-ADMIN receives 403.
- Suspended Executor cannot be newly selected, accept a pending assignment, submit a death certificate, start death handover, or deliver a handover item.
- ADMIN can view legal-case metadata but `POST /api/legal-cases/{id}/decision` as ADMIN is still forbidden.
- ADMIN payment report matches `ServicePayments` and `WalletTransactions`; report endpoints are read-only.
- Call mock top-up twice with the same `Idempotency-Key`: balance increases once. Call with a new key: balance increases again.
- Confirm wallet, legal, handover, profile/review, DMS simulation, admin suspension and dispute actions appear in `GET /api/admin/audit-logs`.

## Verification limitation

This environment does not have the .NET SDK, so `dotnet build`, EF migration generation, and live PostgreSQL/Supabase tests could not be run here. Run these locally before merging and send any build error output for correction.
