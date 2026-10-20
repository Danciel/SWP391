import { request } from "./client";

// Builds "?a=1&b=2", skipping empty values.
const qs = (params = {}) => {
    const q = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => {
        if (v !== undefined && v !== null && v !== "") q.append(k, v);
    });
    const s = q.toString();
    return s ? `?${s}` : "";
};

// ---- ADMIN only (JWT must carry role ADMIN) ----
export const listAccounts = ({ page = 1, pageSize = 20, search, role } = {}) =>
    request(`/accounts${qs({ page, pageSize, search, role })}`);
export const getAccount = (id) => request(`/accounts/${id}`);
export const createAccount = ({ fullName, email, phoneNumber, password, role = "USER", emailVerified = false, phoneVerified = false }) =>
    request("/accounts", { method: "POST", body: { fullName, email, phoneNumber, password, role, emailVerified, phoneVerified } });
// Partial update: only the fields you pass are sent/changed.
export const updateAccount = (id, { fullName, email, phoneNumber, password, role, emailVerified, phoneVerified, identityStatus }) =>
    request(`/accounts/${id}`, { method: "PUT", body: { fullName, email, phoneNumber, password, role, emailVerified, phoneVerified, identityStatus } });
export const deleteAccount = (id) => request(`/accounts/${id}`, { method: "DELETE" });