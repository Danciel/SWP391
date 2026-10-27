# Scoped update: Asset details and deletion; declined Will deletion

Frontend:
- Owner Asset details use existing GET /api/assets/{id}/secret, show username, hide password until Show clicked, clear revealed data on close.
- Add confirmation and server-side deletion for unused Assets.
- Allow deletion of owner Will DRAFT (existing endpoint) or one explicitly DECLINED by executor (new endpoint), without allowing ACTIVE or pending assignments to be deleted.

Backend (old source unchanged):
- Added AssetMaintenanceController.cs: DELETE /api/asset-maintenance/{id} checks ownership and verifies no WillAssets reference, then invokes existing AssetService.DeleteAsync.
- Added WillMaintenanceController.cs: DELETE /api/will-maintenance/{id}/declined checks owner, PENDING_EXECUTOR and ExecutorAssignment.Status=DECLINED, then removes Will (existing cascades).

Notes:
- No database/schema changes. Existing endpoint implementations unchanged.
- Actual .NET/React end-to-end execution has NOT been tested here. Frontend npm dependencies are unavailable offline. Verify on a development machine.
- Secrets are only retrieved from Owner-authorized API, but browser developer tools can inspect requests. Do not store real-world credentials in development mock accounts.
