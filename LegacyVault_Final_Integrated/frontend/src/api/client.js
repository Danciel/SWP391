export const BASE_URL =
  import.meta.env.VITE_API_URL || "https://localhost:7015/api";

export const MOCK_MODE =
  String(import.meta.env.VITE_MOCK_MODE ?? "false").toLowerCase() === "true";

export const ADVANCED_MOCK_MODE =
  String(import.meta.env.VITE_ADVANCED_MOCK_MODE ?? "true").toLowerCase() === "true";

const TOKEN_KEY = "lv_token";
const USER_KEY = "lv_user";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

export function getStoredUser() {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function setStoredUser(user) {
  if (user) localStorage.setItem(USER_KEY, JSON.stringify(user));
  else localStorage.removeItem(USER_KEY);
}

export async function request(path, { method = "GET", body, auth = true } = {}) {
  const headers = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";

  if (auth) {
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  let res;
  try {
    res = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(
      `Could not reach the backend at ${BASE_URL}. Start LegacyVault backend and check CORS/HTTPS certificate.`,
      0,
      null
    );
  }

  let data = null;
  if (res.status !== 204) {
    try {
      data = await res.json();
    } catch {
      data = null;
    }
  }

  if (!res.ok) {
    const message = data?.message || `Error ${res.status}`;
    throw new ApiError(message, res.status, data);
  }

  return data;
}

export class ApiError extends Error {
  constructor(message, status, payload) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.payload = payload;
  }
}
