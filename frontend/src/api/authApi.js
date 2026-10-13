import { request, setToken, MOCK_MODE } from "./client";
import * as mock from "./mockBackend";

const notSupported = (name) =>
    Promise.reject(new Error(`${name} is not supported by the backend yet.`));

// username is required by the backend. Pass one from the form; otherwise derive from the email.
const deriveUsername = (email) =>
    (email.split("@")[0].replace(/[^A-Za-z0-9_.-]/g, "") || "user").slice(0, 50).padEnd(3, "_");

export async function registerOwner({ fullName, email, phoneNumber, password, username }) {
  if (MOCK_MODE) return mock.registerOwner({ fullName, email, phoneNumber, password });

  return request("/auth/register", {
    method: "POST",
    auth: false,
    body: {
      username: username || deriveUsername(email),
      email,
      phone: phoneNumber || null,
      password,
      role: "OWNER",
      fullName: fullName || null,
    },
  });
}

// `identifier` may be a username, email or phone. `email` is still accepted for existing callers.
export async function login({ email, identifier, password }) {
  if (MOCK_MODE) {
    const data = await mock.login({ email, password });
    setToken(data.token);
    return data;
  }

  const data = await request("/auth/login", {
    method: "POST",
    auth: false,
    body: { identifier: identifier ?? email, password },
  });
  setToken(data.accessToken);
  // AuthContext reads `data.user`; the backend names it `account`.
  return { ...data, token: data.accessToken, user: data.account };
}

export function logout() {
  setToken(null);
}

// NOTE: the backend verifies OTPs for the *logged-in* user, so the user must be logged in first.
// purpose: "register" | "email" -> email OTP, "phone" -> phone OTP. `email` is ignored by the backend.
export async function verifyOtp({ email, otp, purpose = "register" }) {
  if (MOCK_MODE) return mock.verifyOtp({ email, otp, purpose });

  const path = purpose === "phone" ? "/identity/verify-phone" : "/auth/verify-email-otp";
  return request(path, { method: "POST", body: { code: otp } });
}

export async function resendOtp({ email, purpose = "register" }) {
  if (MOCK_MODE) return mock.resendOtp({ email, purpose });

  const path = purpose === "phone" ? "/identity/send-phone-otp" : "/auth/send-email-otp";
  return request(path, { method: "POST" });
}

// No backend endpoints exist for these two yet.
export async function requestPasswordReset({ email }) {
  if (MOCK_MODE) return mock.requestPasswordReset({ email });
  return notSupported("requestPasswordReset");
}

export async function resetPassword({ email, resetToken, newPassword }) {
  if (MOCK_MODE) return mock.resetPassword({ email, resetToken, newPassword });
  return notSupported("resetPassword");
}