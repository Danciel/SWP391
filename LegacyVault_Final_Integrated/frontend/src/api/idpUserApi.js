import { request } from "./client";

// Builds "?a=1&b=2", skipping empty values (copied from accountApi.js)
const qs = (params = {}) => {
    const q = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => {
        if (v !== undefined && v !== null && v !== "") q.append(k, v);
    });
    const s = q.toString();
    return s ? `?${s}` : "";
};

// ==========================================
// ADMIN ONLY (JWT must carry role "ADMIN")
// ==========================================

/**
 * List IdpUsers with pagination, search, and optional status filter.
 * @param {Object} params
 * @param {number} [params.page=1]
 * @param {number} [params.pageSize=20]
 * @param {string} [params.search] - Matches FullName, Email, or Phone
 * @param {string} [params.identityStatus] - e.g., "UNVERIFIED", "VERIFIED", "ALIVE"
 */
export const listIdpUsers = ({ page = 1, pageSize = 20, search, identityStatus } = {}) =>
    request(`/idp-users${qs({ page, pageSize, search, identityStatus })}`);

/**
 * Get a single IdpUser by their GUID.
 * @param {string} id
 */
export const getIdpUser = (id) =>
    request(`/idp-users/${id}`);

/**
 * Search for an IdpUser by exactly ONE of email or phone (not both).
 * @param {Object} params
 * @param {string} [params.email]
 * @param {string} [params.phone]
 * @returns {Promise<{id: string, fullName: string, maskedEmail: string, maskedPhoneNumber: string, identityStatus: string}>}
 */
export const searchIdpUsers = ({ email, phone } = {}) =>
    request(`/idp-users/search${qs({ email, phone })}`);

/**
 * Create a new IdpUser.
 * Note: FaceId is intentionally omitted (managed by the Face service).
 * @param {Object} data
 * @param {string} data.fullName
 * @param {string} data.email
 * @param {string} [data.phoneNumber]
 * @param {string} [data.identityStatus="UNVERIFIED"]
 */
export const createIdpUser = ({ fullName, email, phoneNumber, identityStatus = "UNVERIFIED" }) =>
    request("/idp-users", {
        method: "POST",
        body: { fullName, email, phoneNumber, identityStatus }
    });

/**
 * Partially update an existing IdpUser.
 * Only the fields you pass will be sent and changed.
 * @param {string} id
 * @param {Object} data
 * @param {string} [data.fullName]
 * @param {string} [data.email]
 * @param {string} [data.phoneNumber]
 * @param {string} [data.identityStatus]
 */
export const updateIdpUser = (id, { fullName, email, phoneNumber, identityStatus }) =>
    request(`/idp-users/${id}`, {
        method: "PUT",
        body: { fullName, email, phoneNumber, identityStatus }
    });

/**
 * Delete an IdpUser.
 * @param {string} id
 */
export const deleteIdpUser = (id) =>
    request(`/idp-users/${id}`, { method: "DELETE" });