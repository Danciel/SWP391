# LegacyVault FE

React + Vite (JS) + CSS Modules. Current scope: Register, Login, OTP Verification, Forgot/Reset Password, Owner Dashboard.

## Mock mode (no backend needed)

By default `MOCK_MODE = true` in `src/api/client.js`. This routes every call in
`authApi.js` / `ownerApi.js` to `src/api/mockBackend.js`, a fake backend that
stores users, OTP codes, and reset tokens in `localStorage` and returns a
sample dashboard. This lets you exercise the full flow (Register → Verify OTP
→ Login → Dashboard, and Forgot → Verify OTP → Reset Password) without a real
API.

- OTP codes are logged to the browser console each time they're issued, and
  `123456` is **always** accepted as a shortcut so you don't have to check
  the console every time.
- Data persists across reloads (it's just localStorage) but is entirely
  local to your browser — clear site data / localStorage to reset it.
- Switch `MOCK_MODE` to `false` in `src/api/client.js` once your real backend
  is ready; nothing else needs to change, since `authApi.js`/`ownerApi.js`
  fall back to hitting `BASE_URL` in that case.

## Run

```bash
npm install
npm run dev
```

Update `BASE_URL` in `src/api/client.js` to point to the real backend.

## Structure

```
src/
  api/
    client.js
    authApi.js
    ownerApi.js
  context/
    AuthContext.jsx
  components/
    FormField/
    ProtectedRoute.jsx
  pages/
    Login/
    Register/
    VerifyOtp/
    ForgotPassword/
    ResetPassword/
    OwnerDashboard/
  utils/
    validation.js
```

## API expected from backend

- `POST /auth/register` body `{ fullName, email, phoneNumber, password, role }`
  → `{ user }`
- `POST /auth/login` body `{ email, password, rememberMe }`
  → `{ token, user }`
- `POST /auth/verify-otp` body `{ email, otp, purpose }`
  → `{ resetToken? }` (returned when `purpose` is `"reset"`)
- `POST /auth/resend-otp` body `{ email, purpose }`
  → `{}`
- `POST /auth/forgot-password` body `{ email }`
  → `{}`
- `POST /auth/reset-password` body `{ email, resetToken, newPassword }`
  → `{}`
- `GET /owner/dashboard` (Header `Authorization: Bearer <token>`)
  → `{ vaultStatus, deadManSwitch, assets, executors, beneficiaries, recentActivity }`

## Client-side validation

- Full name: not empty, ≥ 2 characters
- Email: valid format
- Phone number: valid format
- Password: ≥ 8 characters, with uppercase, lowercase, and a number
- Confirm password: must match password
- OTP: 6 digits
- Terms checkbox: must be checked before registering

This is for UX only — the backend must still validate everything server-side.
