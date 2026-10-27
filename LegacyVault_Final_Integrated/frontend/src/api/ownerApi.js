
import { getToken, request } from "./client"; // Use your existing client helper
import { getOwnerDashboardData, checkInDms, cancelDeathHandover, requestImmediateHandover } from "./vaultApi";

export const getOwnerDashboard = getOwnerDashboardData;
export const confirmStillActive = ({ userId, willId }) => checkInDms(userId, willId);
export const cancelHandover = ({ userId, willId }) => cancelDeathHandover(userId, willId);
export const requestHandover = ({ userId, willId }) => requestImmediateHandover(userId, willId);

export const linkIdp = async (externalIdentityId, faceImageBlob) => {
    if (!externalIdentityId) throw new Error("External Identity ID is required");
    if (!faceImageBlob) throw new Error("Face image is required");

    const form = new FormData();
    form.append("ExternalIdentityId", externalIdentityId);
    form.append("FaceImage", faceImageBlob, "face.jpg");

    const headers = {};
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    // DO NOT set Content-Type header here. The browser will do it automatically.

    // Use a relative URL so the Vite Proxy handles the backend routing
    const url = `/api/accounts/link-idp`;

    try {
        const res = await fetch(url, { method: "POST", headers, body: form });
        const data = await res.json().catch(() => null);

        if (!res.ok) {
            const error = new Error(
                data?.message || data?.title || `Error ${res.status}`
            );

            error.status = res.status;
            error.data = data;

            throw error;
        }
        return data;
    } catch (err) {
        throw new Error(err.message || "Could not reach the server.");
    }
};