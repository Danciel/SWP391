const env = import.meta.env;

if (env.PROD && !env.VITE_API_URL) {
  throw new Error("VITE_API_URL must be set for production builds.");
}

const rawBase = (import.meta.env.VITE_API_URL || "https://localhost:7202/api")
    .replace(/\/+$/, "");

if (import.meta.env.DEV && !import.meta.env.VITE_API_URL) {
  console.warn("VITE_API_URL is not set; falling back to https://localhost:7202/api");
}

if (!/^https?:\/\//.test(rawBase) && !rawBase.startsWith("/")) {
  throw new Error(
      `Invalid VITE_API_URL "${rawBase}". It must start with http://, https://, or /.`
  );
}

export const BASE_URL = rawBase;

export const MOCK_MODE =
    String(env.VITE_MOCK_MODE ?? "false").toLowerCase() === "true";

console.log("MODE:", import.meta.env.MODE, "API:", import.meta.env.VITE_API_URL);

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
  const headers = { "Content-Type": "application/json" };
  if (auth) {
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  let res;
  try {
    res = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
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
    // Some successful endpoints may intentionally return no JSON body.
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
