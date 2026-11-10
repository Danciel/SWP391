# Patches to Step 1+2, and Step 4 (DMS = TRIGGERED)

No SQL has to be run by hand any more: on start the backend runs `Data/SchemaBootstrap.cs`
(idempotent, same SQL as `s10_handover_access_codes.sql`) and renames old DMS rows
`THRESHOLD_REACHED` -> `TRIGGERED`. The `.sql` file is still there if you prefer to run it yourself.

## Patches

| # | Problem | Fix |
|---|---------|-----|
| 1 | s10 not run -> recipient page 500 | Table is created at startup. If that fails the app still starts and logs the reason. |
| 2 | `OwnerPin:Pepper` missing -> code never sent, recipient told "Executor has not delivered" | Startup logs an ERROR. **Deliver / Deliver all now answer 400 with a clear message BEFORE changing anything.** Recipient screens say "temporarily unavailable" (503) or `accessCode.status = UNAVAILABLE`, never the wrong sentence. |
| 3 | Executor card had no recipient status | Each asset row shows: waiting for registration, name does not match, not delivered, code not issued, email not sent, code sent, locked until..., confirmed, received. Source: `items[].recipientStatus` of `GET /api/handover/{willId}` (Owner/Executor only). |
| 4 | Executor could not resend a code | `POST /api/handover/{willId}/recipients/{designationId}/resend-code` + button **Send a new code**. 5 min cooldown, hard limit 10 codes per recipient, audited, only goes to the recipient's verified email. Not available while the recipient is unregistered. |
| 5 | After 5 codes there was no way out | `POST /api/inheritance/{willId}/access-code/support` + button **Ask for help** (shown when 5 codes were used or the code is locked). Emails the Executor, writes audit `HANDOVER_CODE_SUPPORT_REQUESTED` (admins see it in the Audit Log), once per 24 h. The Executor row then says the recipient asked for help. |
| 6 | Build risk | `f.Box` checked against the FaceAiSharp source: `FaceDetectorResult(RectangleF Box, ...)`, so it is valid. The new `ExecuteUpdateAsync` calls use the same `SetProperty(x => x.Prop, value)` form already used (and compiled) in `HandoverService`. Frontend builds with `vite build`. **The backend was NOT fully compiled here (no NuGet access): please run `dotnet build` once.** |

## Step 4: DMS / death certificate

One state machine: `WAITING -> ACTIVE -> TRIGGERED`. `THRESHOLD_REACHED` is gone (alias kept in the admin API).

- One method moves a DMS to TRIGGERED: `DmsMissedCycleWorker.MarkTriggered` (used by the hourly job and by Admin). It writes audit `DMS_TRIGGERED` with the source (`MISSED_CHECK_INS`, `ADMIN_FORCED`, `ADMIN_CONFIGURE`).
- Check-in (Owner): refused with a clear message once TRIGGERED (an "I'm alive" flow is in round 2).
- Alerts (`GET /api/dms/alerts`) now include TRIGGERED (`triggered: true`, label "Triggered"); the top bar shows "DMS triggered, your Executor was notified".
- Admin Dashboard: new **Dead Man's Switch** card: list, **Run evaluation now**, **Force trigger** (reason required, min 3 chars).
  `POST /api/admin/dms/{willId}/force-trigger { reason }`: only for an enabled DMS of an ACTIVE will that is not already triggered. It emails the Executor at once (same mail as the automatic trigger; retried hourly if it fails). It does NOT mark the Owner deceased and does NOT start a handover: Executor -> certificate -> Legal Verify -> LifeStatus DECEASED is still required.
- `POST /api/admin/dms/{willId}/configure` accepts `ACTIVE` or `TRIGGERED`.

## How to test
1. Start the backend WITHOUT running s10: the recipient page works (no 500).
2. Remove `OwnerPin:Pepper`: log shows the ERROR; **Deliver all** answers 400 with the message; nothing is delivered.
3. Deliver all with a registered recipient: Executor row shows "Code sent, waiting for the recipient"; after 5 minutes press **Send a new code**.
4. As the recipient use "Send me a new code" 5 times (5 min apart; or change `ResendCooldownMinutes` for the test): "Ask for help" appears; press it; the Executor gets the email and the row shows the request.
5. Admin -> Dead Man's Switch -> Force trigger: Will shows Triggered, Executor receives the email and sees the trigger in "DMS triggers"; the Owner sees the top-bar notice.

## Security notes found while reading (not changed here)
`appsettings.Development.json` in this zip contains the real Supabase password, the Gmail app password, `Jwt:Key` and `Encryption:KekBase64`. Treat them as leaked: rotate them and move them to `dotnet user-secrets`. Also `Encryption:KekBase64` contains spaces, so it is not valid base64 as written. (Planned for round 2.)
