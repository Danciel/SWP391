export const BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

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

export async function request(path, { method = "GET", body, auth = true, headers: extraHeaders } = {}) {
  const isFormData = body instanceof FormData;
  const headers = { ...(extraHeaders || {}) };

  // JSON chỉ được khai báo Content-Type khi không gửi FormData.
  if (!isFormData) {
    headers["Content-Type"] = "application/json";
  }

  // Giữ nguyên cơ chế xác thực hiện tại.
  if (auth) {
    const token = getToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
  }

  let res;

  try {
    res = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body:
        body == null
          ? undefined
          : isFormData
            ? body
            : JSON.stringify(body),
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
    // Response không có JSON.
  }

  if (!res.ok) {
    const validation = data?.errors ? Object.values(data.errors).flat()[0] : null;
    const message = data?.message || validation || data?.title || `Error ${res.status}`;
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
