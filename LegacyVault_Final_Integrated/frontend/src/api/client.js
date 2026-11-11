export const BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

const TOKEN_KEY = "lv_token";
const USER_KEY = "lv_user";
const REFRESH_KEY = "lv_refresh";

// Fired when the session can no longer be used (account locked, token revoked/expired).
// AuthContext listens to it and sends the user back to /login.
export const SESSION_ENDED_EVENT = "lv:session-ended";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

export function getRefreshToken() {
  return localStorage.getItem(REFRESH_KEY);
}

export function setRefreshToken(token) {
  if (token) localStorage.setItem(REFRESH_KEY, token);
  else localStorage.removeItem(REFRESH_KEY);
}

export function endSession(reason, quiet = false) {
  setToken(null);
  setRefreshToken(null);
  setStoredUser(null);
  window.dispatchEvent(new CustomEvent(SESSION_ENDED_EVENT, { detail: { reason, quiet } }));
}

// One refresh request at a time; parallel 401s share the same promise.
let refreshing = null;

function refreshAccessToken() {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return Promise.resolve(false);
  if (!refreshing) {
    refreshing = (async () => {
      try {
        const res = await fetch(`${BASE_URL}/auth/refresh`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken }),
        });
        if (!res.ok) return false;
        const data = await res.json();
        if (!data?.token) return false;
        setToken(data.token);
        // Token rotation: the old refresh token is dead, always store the new one.
        if (data.refreshToken) setRefreshToken(data.refreshToken);
        return true;
      } catch {
        return false;
      }
    })().finally(() => {
      refreshing = null;
    });
  }
  return refreshing;
}

function isAccountLocked(status, data, message) {
  if (data?.code === "ACCOUNT_LOCKED") return true;
  // Fallback while the backend has no error code yet.
  return (status === 401 || status === 403) && /account.*\blocked\b/i.test(message || "");
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

export async function request(
  path,
  { method = "GET", body, auth = true, headers: extraHeaders, retried = false, handleSession = true, quietExpiry = false } = {}
) {
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
    let message = data?.message || validation || data?.title || `Error ${res.status}`;

    // N4: rate limit / brute-force protection.
    let retryAfter = null;
    if (res.status === 429) {
      retryAfter =
        Number(res.headers.get("Retry-After")) || Number(data?.retryAfterSeconds) || null;
      if (!data?.message) {
        message = retryAfter
          ? `Too many requests. Please try again in ${retryAfter}s.`
          : "Too many requests. Please wait a moment and try again.";
      }
    }

    if (auth && handleSession) {
      // N2/N3: a locked account must lose its (old) JWT immediately.
      if (isAccountLocked(res.status, data, message)) {
        endSession("locked");
      } else if (res.status === 401 && !retried) {
        // N5: access token expired -> rotate once, then replay the request.
        if (await refreshAccessToken()) {
          return request(path, { method, body, auth, headers: extraHeaders, retried: true, handleSession, quietExpiry });
        }
        endSession("expired", quietExpiry);
      } else if (res.status === 401) {
        endSession("expired", quietExpiry);
      }
    }

    throw new ApiError(message, res.status, data, { retryAfter });
  }

  return data;
}

export class ApiError extends Error {
  constructor(message, status, payload, extra = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.payload = payload;
    this.code = payload?.code || null;
    this.retryAfter = extra.retryAfter ?? null;
  }
}
