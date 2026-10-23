import {getOwnerDashboardData,checkInDms,cancelDeathHandover,requestImmediateHandover} from "./vaultApi";
export const getOwnerDashboard=getOwnerDashboardData;
export const confirmStillActive=({userId,willId})=>checkInDms(userId,willId);
export const cancelHandover=({userId,willId})=>cancelDeathHandover(userId,willId);
export const requestHandover=({userId,willId})=>requestImmediateHandover(userId,willId);


export const linkIdp = async (externalIdentityId, faceImageBlob) => {
    if (!externalIdentityId) throw new Error("External Identity ID is required");
    if (!faceImageBlob) throw new Error("Face image is required");

    const form = new FormData();
    form.append("ExternalIdentityId", externalIdentityId);
    form.append("FaceImage", faceImageBlob, "face.jpg");

    const headers = {};
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
    // NOTE: Do not set Content-Type manually for FormData

    const url = `${BASE_URL}/accounts/link-idp`; // Hits LegacyVault API

    try {
        const res = await fetch(url, { method: "POST", headers, body: form });
        const data = await res.json().catch(() => null);

        if (!res.ok) {
            throw new ApiError(data?.message || "Link IDP failed", res.status, data);
        }
        return data;
    } catch (err) {
        if (err instanceof ApiError) throw err;
        throw new ApiError(`Could not reach the server at ${BASE_URL}`, 0, null);
    }
};