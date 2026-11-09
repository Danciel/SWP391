# Frontend integration – backend unchanged

- Backend is copied byte-for-byte from `LegacyVault_Final_Integrated(1).zip`.
- Frontend UI is taken from `legacyvault-fe (2)(1).zip`; only frontend API adapters/configuration are revised.
- Local mock modules `mockBackend.js` and `mockVault.js` are absent.
- Implemented backend routes: `/api/auth/{register,login,verify-otp,resend-otp}`, `/api/accounts/{me,search}`, `/api/assets`, `/api/wills` (assets, beneficiaries, assignments, executor, dms, submit), `/api/executors`.
- Features absent from backend have empty read models or explicit 501 errors, not fake persisted data.

## Running without changing backend CORS

1. Run the backend on its configured HTTP profile (`http://localhost:5161` in original `launchSettings.json`).
2. Run `npm install` then `npm run dev` in `frontend`.
3. Open the URL printed by Vite. Its `/api` requests are proxied to port 5161; requests remain same-origin in browser.
4. If the **actual backend port differs**, set `VITE_BACKEND_TARGET` to its origin (for example `http://localhost:7015`) before `npm run dev`, or create `frontend/.env.local` with `VITE_BACKEND_TARGET=http://localhost:7015`. Restart Vite after a change.
5. Do not set `VITE_API_BASE_URL` to a cross-origin URL unless CORS was already enabled for the Vite origin.

## Verification limitations

Code and routes have been cross-checked statically. No SQL Server or live backend login was available for end-to-end testing. Unsupported features remain unimplemented; buttons for these features can report an error rather than completing the action.
