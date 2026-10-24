import { getToken, ApiError } from "./client";

const IDP_BASE_URL = (
    import.meta.env.VITE_IDP_API_URL || "http://localhost:5100/api"
).replace(/\/+$/, "");

const qs = (params = {}) => {
    const q = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => {
        if (v !== undefined && v !== null && v !== "") q.append(k, v);
    });
    const s = q.toString();
    return s ? `?${s}` : "";
};

// Custom request helper for MockIdpApi

async function idpRequest(path, { method = "GET", body } = {}) {
    const headers = {};
    const token = getToken();

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const url = `${IDP_BASE_URL}${path}`;

    try {
        const isFormData = body instanceof FormData;

        if (!isFormData) {
            headers["Content-Type"] = "application/json";
        }

        const res = await fetch(url, {
            method,
            headers,
            body: body
                ? isFormData
                    ? body
                    : JSON.stringify(body)
                : undefined,
        });

        const data = await res.json().catch(() => null);

        if (!res.ok) {
            throw new ApiError(
                data?.message || `Error ${res.status}`,
                res.status,
                data
            );
        }

        return data;
    } catch (err) {
        if (err instanceof ApiError) throw err;

        throw new ApiError(
            `Could not reach the Mock IDP at ${IDP_BASE_URL}`,
            0,
            null
        );
    }
}


export const listIdpUsers = ({ page = 1, pageSize = 20, search, identityStatus } = {}) =>
    idpRequest(`/idp-users${qs({ page, pageSize, search, identityStatus })}`);

export const getIdpUser = (id) =>
    idpRequest(`/idp-users/${id}`);

export const searchIdpUsers = ({ email, phone } = {}) =>
    idpRequest(`/idp-users/search${qs({ email, phone })}`);

export const createIdpUser = ({ fullName, email, phoneNumber, identityStatus = "UNVERIFIED", lifeStatus = "ALIVE" }) =>
    idpRequest("/idp-users", {
        method: "POST",
        body: { fullName, email, phoneNumber, identityStatus, lifeStatus }
    });

export const updateIdpUser = (id, { fullName, email, phoneNumber, identityStatus, lifeStatus }) =>
    idpRequest(`/idp-users/${id}`, {
        method: "PUT",
        body: { fullName, email, phoneNumber, identityStatus, lifeStatus }
    });

export const deleteIdpUser = (id) =>
    idpRequest(`/idp-users/${id}`, { method: "DELETE" });

export const enrollIdpFace = (idpUserId, faceImage) => {
    const formData = new FormData();
    formData.append("FaceImage", faceImage);

    return idpRequest(
        `/idp-users/${encodeURIComponent(idpUserId)}/face/enroll`,
        {
            method: "POST",
            body: formData,
        }
    );
};