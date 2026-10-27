
import { request } from "./client";

/**
 * Admin Account CRUD API
 *
 * Backend base route: /api/accounts
 * client.js already handles BASE_URL and JWT Authorization.
 */

const ACCOUNT_ROLES = [
  "USER",
  "ADMIN",
  "OWNER",
  "BENEFICIARY",
  "LEGAL_VERIFIER",
  "EXECUTOR",
];

const hasOwn = (object, key) =>
  Object.prototype.hasOwnProperty.call(object, key);

function normalizeAccountList(response) {
  // Backend trả trực tiếp một mảng
  if (Array.isArray(response)) {
    return response;
  }

  // Backend trả ServiceResult<PagedResponse<AccountResponse>>
  const data = response?.data;

  if (Array.isArray(data?.items)) {
    return data.items;
  }

  // Hỗ trợ các định dạng khác nếu cần tương thích
  if (Array.isArray(response?.items)) {
    return response.items;
  }

  if (Array.isArray(response?.accounts)) {
    return response.accounts;
  }

  if (Array.isArray(data)) {
    return data;
  }

  return [];
}

function buildQuery(params = {}) {
  const query = new URLSearchParams();

  query.set("page", String(params.page ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));

  if (params.search?.trim()) {
    query.set("search", params.search.trim());
  }

  if (params.role?.trim()) {
    query.set("role", params.role.trim());
  }

  return query.toString();
}

function validateRole(role) {
  if (role == null) return;

  const normalizedRole = String(role).trim().toUpperCase();

  if (!ACCOUNT_ROLES.includes(normalizedRole)) {
    throw new Error(
      `Invalid role. Allowed roles: ${ACCOUNT_ROLES.join(", ")}`
    );
  }
}

/**
 * GET /api/accounts?page=1&pageSize=20&search=...&role=...
 */
export async function listAccounts(params = {}) {
  const response = await request(`/accounts?${buildQuery(params)}`);
  return response;
}

/**
 * GET /api/accounts/{id}
 */
export function getAccount(accountId) {
  if (!accountId) {
    throw new Error("Account ID is required.");
  }

  return request(`/accounts/${encodeURIComponent(accountId)}`);
}

/**
 * POST /api/accounts
 */
export function createAccount(account) {
  if (!account) {
    throw new Error("Account data is required.");
  }

  const {
    fullName,
    email,
    phoneNumber,
    password,
    role = "USER",
    emailVerified = false,
    phoneVerified = false,
  } = account;

  if (!fullName?.trim()) {
    throw new Error("Full name is required.");
  }

  if (!email?.trim()) {
    throw new Error("Email is required.");
  }

  if (typeof password !== "string" || password.length < 8) {
    throw new Error("Password must contain at least 8 characters.");
  }

  validateRole(role);

  return request("/accounts", {
    method: "POST",
    body: {
      fullName: fullName.trim(),
      email: email.trim(),
      phoneNumber: phoneNumber?.trim() || null,
      password,
      role: String(role).toUpperCase(),
      emailVerified,
      phoneVerified,
    },
  });
}

/**
 * PUT /api/accounts/{id}
 * Only supplied fields are sent to the backend.
 */
export function updateAccount(accountId, changes = {}) {
  if (!accountId) {
    throw new Error("Account ID is required.");
  }

  const allowedFields = [
    "fullName",
    "email",
    "phoneNumber",
    "password",
    "role",
    "emailVerified",
    "phoneVerified",
    "identityStatus",
  ];

  const body = {};

  for (const field of allowedFields) {
    if (hasOwn(changes, field) && changes[field] !== undefined) {
      body[field] = changes[field];
    }
  }

  if (hasOwn(body, "role")) {
    validateRole(body.role);
    body.role = String(body.role).toUpperCase();
  }

  if (
    hasOwn(body, "password") &&
    body.password != null &&
    body.password !== "" &&
    body.password.length < 8
  ) {
    throw new Error("Password must contain at least 8 characters.");
  }

  return request(`/accounts/${encodeURIComponent(accountId)}`, {
    method: "PUT",
    body,
  });
}

/**
 * DELETE /api/accounts/{id}
 * Backend returns 204 No Content on success.
 */
export function deleteAccount(accountId) {
  if (!accountId) {
    throw new Error("Account ID is required.");
  }

  return request(`/accounts/${encodeURIComponent(accountId)}`, {
    method: "DELETE",
  });
}

/**
 * Compatibility with the existing AdminDashboard.
 * The AccountsController does not provide dashboard statistics,
 * cases, payments, or audit log endpoints.
 */
export async function getAdminDashboard() {
  const response = await listAccounts({ page: 1, pageSize: 100 });
  const accounts = normalizeAccountList(response);

  return {
    accounts,
    cases: [],
    payments: [],
    auditLog: [],
    stats: {
      totalUsers: accounts.length,
      totalWills: 0,
      totalAssets: 0,
    },
    recentActivity: [],
  };
}

/**
 * Compatibility with existing callers.
 * Role changes use the real PUT account endpoint.
 */
export function setAccountRole({ accountId, role }) {
  return updateAccount(accountId, { role });
}

/**
 * No account status endpoint exists in the supplied AccountsController.
 * Do not send requests to a nonexistent /status endpoint.
 */
export function setAccountStatus() {
  throw new Error(
    "Account status management is not supported by the current AccountsController API."
  );
}
