# Face verification fix

Symptom: Verify Identity > Face always showed "Face verification failed ..." even though the
face matched in MockIdpApi Swagger (`matched: true`).

Causes
1. LegacyVault called MockIdpApi at `https://localhost:7176/` (default), but MockIdpApi was run with the
   `http` profile (port 5100 only) -> connection refused -> swallowed as `false`.
2. Every failure (IdP unreachable, no enrolled face, no/multiple faces, mismatch) was collapsed into one
   boolean and the frontend replaced it with a fixed sentence, so the real reason was never visible.
3. MockIdpApi rejected any photo that contained more than one face (e.g. people in the background).

Changes (same files / layers / routes as before)
- `IIdpIntegrationService` / `IdpIntegrationService`: `VerifyFaceAsync` and `LinkIdpUserAsync` now return
  `IdpFaceResult` (Success, StatusCode, Message) instead of `bool`.
  Base URL: `IdpApiSettings:BaseUrl` -> `Idp:BaseUrl` -> `http://localhost:5100/` (same default as IdpLifeStatusClient).
  30 s timeout; errors are logged with structure.
- `AccountsController.LinkIdp` and `InheritanceController` (claim with face) return the real message/status.
- `MockIdpApi/FaceRecognitionService`: uses the dominant face (rejects only if a second face is > 50% of its
  area); corrupt images -> 422 instead of 500.
- Frontend: `VerifyIdentity.jsx` shows the backend message; `faceRecognitionApi.js` reads `VITE_IDP_API_URL`
  (default `http://localhost:5100/api`, was hard-coded `https://localhost:7176/api`).
- `appsettings.json`: explicit `IdpApiSettings:BaseUrl`.

After pulling: stop MockIdpApi AND LegacyVault (a running MockIdpApi locks `MockIdpApi.exe`), then start both.
