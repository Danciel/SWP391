import { request } from "./client";

// Calls the EXISTING backend endpoint POST /api/assets/{id}/verify.
// Kept in its own file so vaultApi.js (the agreed API adapter) stays untouched.
// Response: { assetId, status: "VERIFIED" | "FAILED", message }
export const verifyAsset = (assetId) => request(`/assets/${assetId}/verify`, { method: "POST" });
