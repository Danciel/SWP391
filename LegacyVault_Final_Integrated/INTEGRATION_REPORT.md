# Integration Report

## Backend changes for FE integration

- Added CORS policy for Vite development origins:
  - `http://localhost:5173`
  - `http://127.0.0.1:5173`
- Kept existing JWT/Auth/Asset/Will implementation.

## Frontend Nhật changes

- API base moved from hard-coded `http://localhost:8080/api` to `VITE_API_URL`.
- Main mock mode defaults OFF.
- Advanced modules have separate hybrid mock flag.
- Register no longer sends `role: OWNER`.
- `USER` role added to route/sidebar mapping.
- Profile uses `/api/accounts/me`.
- Asset page uses real Asset APIs, encrypted secret reveal and verification.
- Create Will page uses real Will APIs.
- Beneficiary existing-account search uses one input and routes to exact email/phone backend query.
- My Wills and Will Details use real APIs.
- Executor picker uses `/api/executors` real seeded data.

## Static validation run in merge environment

PASS:

- JS/JSX syntax parsed with TypeScript parser/transpiler.
- All relative frontend imports resolve to existing files.
- Backend source had previously built successfully on Kiệt's Visual Studio before CORS-only modification.

Not executable in merge environment:

- `.NET` SDK is not installed here, so backend build cannot be rerun here.
- `npm install` timed out due environment transport/network, so full Vite build could not be completed here.

Kiệt should run Build Solution + `npm install && npm run dev` locally for runtime verification.
