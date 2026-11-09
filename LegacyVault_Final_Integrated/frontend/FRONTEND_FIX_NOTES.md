# Frontend-only loading fix

- Backend is unchanged.
- Root causes: failed API promises left loading state true in Profile, Assets, MyWills, CreateWill, WillDetails and InheritedWillDetails. Error/finally handling now stops indefinite spinners.
- Actual backend `https` profile in `backend/LegacyVault/Properties/launchSettings.json` is `https://localhost:7015` (HTTP `http://localhost:5161`). Vite default proxy is now HTTPS 7015.
- IMPORTANT: if you previously created `frontend/.env.local` with `https://localhost:8161` or `http://localhost:7015`, replace the value with `VITE_BACKEND_TARGET=https://localhost:7015` or delete the file; Vite needs restart.
- Existing 401 errors still require a valid accepted JWT. The fix does not bypass backend authentication.
- Backend routes and mock providers were not altered.
