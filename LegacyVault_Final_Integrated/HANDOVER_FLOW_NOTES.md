# Handover flow – what changed and how to test

## 0. Run once (Supabase SQL Editor)
`backend/LegacyVault/database/s9_handover_claim_and_certificate.sql`
(adds `HandoverItems.ClaimedAt/ClaimedByAccountId` and `LegalCases.CertificateData`).
Then rebuild the backend (not compiled in the sandbox where this was written).

## 1. Immediate Handover (Owner, still alive)
1. Owner: My Wills (or Will Details) -> **Immediate Handover** -> "Yes, Enter PIN".
2. PIN dialog (scope `HANDOVER_START`, bound to that will). Backend refuses without the grant (HTTP 423).
3. Will -> `HANDOVER_IN_PROGRESS`, a handover case with one item per asset/beneficiary is created.
4. Executor Dashboard -> **Handovers** card -> **Deliver** per asset.
   Last delivery -> case COMPLETED, will HANDED_OVER, the REMAINING fee (total - 20% advance) goes to the Executor wallet.
5. Beneficiary: **View Will** -> Open with PIN (`INHERITANCE_VIEW`) -> **Scan face to receive** -> **View credentials**.

## 2. After death (certificate)
1. Executor Dashboard -> DMS trigger -> **Submit Legal Document** -> fill the **death-certificate form**
   (live preview with seal) or upload a scan. The form is rendered to PNG and uploaded with its data (JSON).
2. Legal Verifier: open the case -> certificate preview, cross-checks (DMS threshold, IdP life status,
   name on certificate vs. Owner), two confirmation boxes -> Approve / Reject (note required).
3. Executor: Legal Cases card -> **Start Handover** (backend re-checks LifeStatus == DECEASED in MockIdpApi).
4. From here identical to section 1 (steps 4-5).

Set the Owner to deceased in MockIdpApi: `PUT /api/idp-users/{id}` with `{ "lifeStatus": "DECEASED" }`.

## Requirements for the beneficiary face scan
* Beneficiary account must be linked to an IdP user with an enrolled face (Verify Identity page).
* MockIdpApi must be running (`IdpApiSettings:BaseUrl`, default https://localhost:7176).
* Beneficiary needs a Security PIN (the modal offers setup on first use).
