import { getToken, ApiError } from "./client";

// Set this in your .env file to match your MockIdpApi port!
// Example: VITE_IDP_API_URL=http://localhost:5100/api
// Default matches idpUserApi.js: the http profile of MockIdpApi (no dev certificate needed).
const IDP_BASE_URL = (
    import.meta.env.VITE_IDP_API_URL || "http://localhost:5100/api"
).replace(/\/+$/, "");

async function postMultipart(path, formData) {
    const headers = {};
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    const url = `${IDP_BASE_URL}${path}`;
    console.log(`[idpFaceApi] POST ${url}`);

    let res;
    try {
        res = await fetch(url, { method: "POST", headers, body: formData });
    } catch (networkErr) {
        throw new ApiError(
            `Could not reach the Mock IDP at ${IDP_BASE_URL}. Make sure it is running.`,
            0,
            null
        );
    }

    let data = null;
    try {
        data = await res.json();
    } catch {
        /* empty or non-JSON body */
    }

    console.log(`[idpFaceApi] POST ${url} -> ${res.status}`, data);

    if (!res.ok) {
        const message = data?.message || `Error ${res.status}`;
        throw new ApiError(message, res.status, data);
    }
    return data;
}

const buildForm = (imageBlob, filename) => {
    const form = new FormData();
    form.append("FaceImage", imageBlob, filename);
    return form;
};

/**
 * Enroll (or replace) a face for the given IdpUser.
 */
export const enrollFace = (idpUserId, imageBlob, filename = "face.jpg") => {
    if (!idpUserId) throw new Error("idpUserId is required");
    if (!imageBlob) throw new Error("imageBlob is required");
    return postMultipart(
        `/idp-users/${idpUserId}/face/enroll`,
        buildForm(imageBlob, filename)
    );
};

/**
 * Verify a face against the stored enrollment.
 */
export const verifyFace = (idpUserId, imageBlob, filename = "face.jpg") => {
    if (!idpUserId) throw new Error("idpUserId is required");
    if (!imageBlob) throw new Error("imageBlob is required");
    return postMultipart(
        `/idp-users/${idpUserId}/face/verify`,
        buildForm(imageBlob, filename)
    );
};

/**
 * Remove the face enrollment for the given IdpUser.
 */
export const deleteFace = async (idpUserId) => {
    if (!idpUserId) throw new Error("idpUserId is required");

    const headers = {};
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    const url = `${IDP_BASE_URL}/idp-users/${idpUserId}/face`;
    const res = await fetch(url, { method: "DELETE", headers });

    if (!res.ok) throw new ApiError(`Error ${res.status}`, res.status, null);
    return null;
};