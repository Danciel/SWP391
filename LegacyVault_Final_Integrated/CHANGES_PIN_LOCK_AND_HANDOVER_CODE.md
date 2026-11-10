# Step 1 + 2: PIN lock (3 tries) and per-recipient handover access code

Apply `backend/LegacyVault/database/s10_handover_access_codes.sql` to the database first
(it is idempotent), then stop MockIdpApi and LegacyVault and start both again.

## 1. Security PIN
- 3 wrong PINs lock the PIN for 15 minutes (was 5). `Verify` and `Change` share the same limits.
- When the lock starts, the account holder gets an email ("Your Security PIN is locked").
  An email problem never changes the PIN response.
- Unlock: wait 15 minutes, or "Forgot PIN" (email OTP) as before.

## 2. Handover access code (one per recipient)
Format `XXXX-XXXX-XXXX`: 11 random symbols (no 0/O, 1/I/L) + 1 check symbol, about 54 bits.
A typo (bad check symbol) is rejected without using one of the 3 attempts.

Flow for the recipient (the will stays hidden until the code is confirmed):
1. "My inherited wills" list: overview only, no PIN needed.
2. Open the will -> Security PIN.
3. Overview only ("You have received a will") + the code box. No description, no asset names,
   no shares are sent by the server yet (`contentLocked: true`, `items: []`).
4. Type the access code from the email -> the will opens (confirmed once = confirmed for good).
5. Face scan to receive each asset -> credentials (Security PIN again).

- The code is generated when the Executor delivers, emailed to the recipient's VERIFIED account
  email, and only a salted + peppered hash is stored (`OwnerPin:Pepper`, same setting as the PIN).
  Nobody can read it back: not the owner, the Executor, an admin or a database reader.
- Executor: new button **Deliver all** (`POST /api/handover/{willId}/deliver-all`). The per-item
  endpoint stays (used after an admin re-opens an item). Both issue the codes.
- Recipient: `POST /api/inheritance/{willId}/access-code/verify` and `.../resend`.
  3 wrong codes lock it for 15 minutes; the recipient and the owner are emailed.
  "Send me a new code" replaces the old code (5 min cooldown, max 5 codes).
- `POST /api/inheritance/items/{itemId}/claim` answers 428 `ACCESS_CODE_REQUIRED` until the code
  was confirmed once (it does not expire). Handovers that started before this change get their
  code the first time the recipient opens the will.
- `GET /api/inheritance/{willId}` returns `accessCode { status, verified, lockedUntil, verifiedUntil }`
  and `contentLocked`. While locked it only returns the overview (title, owner, statuses, counts).
- The access-code endpoints need the Security PIN grant (`X-Owner-Pin-Grant`, scope INHERITANCE_VIEW),
  so the order PIN -> code is enforced by the server, not only by the screen.

### Recipients without an account
- A verified account whose email AND name (ignoring case/accents/spaces) equal the designation
  is linked automatically: when a handover starts, when the person verifies their email, and
  when they sign in. No owner needed (the owner may be deceased).
- A different name is NOT linked automatically (the owner can still use the existing link
  endpoint). The code is only sent once the person is linked and their assets are delivered.
- Email OTP verification now allows 5 tries per code, and "resend" has a 1 minute cooldown
  (auto-link trusts the verified email, so brute-forcing the OTP had to be closed).

## How to test
1. Run `s10_handover_access_codes.sql`. Make sure `OwnerPin:Pepper` is set (32+ chars).
2. Owner: will with assets + a beneficiary (account linked or same email/name), start
   an Immediate handover (PIN).
3. Executor: Handovers card -> **Deliver all**. The beneficiary gets the email with the code.
4. Beneficiary: open the inherited will (PIN) -> type the code -> scan face -> view credentials.
5. Wrong code 3 times -> locked 15 minutes, 2 emails (recipient + owner).
6. Wrong Security PIN 3 times -> locked, email to the account holder.

## Not in this step (next steps)
Signed handover minutes + e-signature, DMS status fix + test tools, deceased-owner account lock,
Executor manual confirmation for name mismatches.
