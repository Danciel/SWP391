// src/api/faceRecognitionApi.js
//
// Client for the IdpUser face endpoints. Reuses BASE_URL, getToken,
// request and ApiError from ./client so it lines up with accountApi.js.
//
//   POST   /idp-users/{idpUserId}/face/enroll   (multipart: FaceImage)
//   POST   /idp-users/{idpUserId}/face/verify   (multipart: FaceImage)
//   DELETE /idp-users/{idpUserId}/face
//
// All three are [Authorize(Roles = "ADMIN")] on the backend.
import { BASE_URL, getToken, request, ApiError } from "./client";

// client.js's request() forces application/json + JSON.stringify,
// so multipart uploads get their own tiny helper that reuses the
// same BASE_URL, token and error contract.
async function postMultipart(path, formData) {
    const headers = {};
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
    // NOTE: do NOT set Content-Type — the browser must add the boundary.

    const url = `${BASE_URL}${path}`;
    console.log(`[faceApi] POST ${url}`);

    let res;
    try {
        res = await fetch(url, { method: "POST", headers, body: formData });
    } catch (networkErr) {
        throw new ApiError(
            `Could not reach the server at ${BASE_URL}. Make sure the backend is running and CORS is configured.`,
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

    console.log(`[faceApi] POST ${url} -> ${res.status}`, data);

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

// ---- ADMIN only ----

/**
 * Enroll (or replace) a face for the given IdpUser.
 * @param {string} idpUserId
 * @param {Blob}   imageBlob  JPEG/PNG
 * @param {string} [filename]
 * @returns {Promise<{ userId: string, faceId: string, faceEnrolled: boolean }>}
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
 * @returns {Promise<{ userId: string, matched: boolean, score: number }>}
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
 * Backend returns 204 No Content on success.
 * @returns {Promise<null>}
 */
export const deleteFace = (idpUserId) => {
    if (!idpUserId) throw new Error("idpUserId is required");
    return request(`/idp-users/${idpUserId}/face`, { method: "DELETE" });
};