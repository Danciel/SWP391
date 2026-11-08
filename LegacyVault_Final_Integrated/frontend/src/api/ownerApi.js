
import { request } from "./client";
import { getOwnerDashboardData, checkInDms, cancelDeathHandover, requestImmediateHandover } from "./vaultApi";

export const getOwnerDashboard = getOwnerDashboardData;
export const confirmStillActive = ({ userId, willId }) => checkInDms(userId, willId);
export const cancelHandover = ({ userId, willId }) => cancelDeathHandover(userId, willId);
export const requestHandover = ({ userId, willId }) => requestImmediateHandover(userId, willId);

// N6: one IdP identity can be linked to one account only. BE answers 409 (code IDP_ALREADY_LINKED).
export const linkIdp = async (externalIdentityId, faceImageBlob) => {
    if (!externalIdentityId) throw new Error("External Identity ID is required");
    if (!faceImageBlob) throw new Error("Face image is required");

    const form = new FormData();
    form.append("ExternalIdentityId", externalIdentityId);
    form.append("FaceImage", faceImageBlob, "face.jpg");

    // request() skips Content-Type for FormData, adds the JWT, refreshes it on 401 and
    // surfaces 409/429 as ApiError(status, code, retryAfter).
    return request("/accounts/link-idp", { method: "POST", body: form });
};
