import { request, setToken, MOCK_MODE } from "./client";
import * as mock from "./mockBackend";

export async function registerOwner({ fullName, email, phoneNumber, password }) {
  if (MOCK_MODE) return mock.registerOwner({ fullName, email, phoneNumber, password });

  return request("/auth/register", {
    method: "POST",
    auth: false,
    body: { fullName, email, phoneNumber, password },
  });
}

export async function login({ email, password, rememberMe }) {
  if (MOCK_MODE) {
    const data = await mock.login({ email, password });
    setToken(data.token);
    return data;
  }

  const data = await request("/auth/login", {
    method: "POST",
    auth: false,
    body: { email, password, rememberMe: !!rememberMe },
  });
  setToken(data.token);
  return data;
}

export function logout() {
  setToken(null);
}

export async function verifyOtp({ email, otp, purpose = "register" }) {
  if (MOCK_MODE) return mock.verifyOtp({ email, otp, purpose });

  return request("/auth/verify-otp", {
    method: "POST",
    auth: false,
    body: { email, otp, purpose },
  });
}

export async function resendOtp({ email, purpose = "register" }) {
  if (MOCK_MODE) return mock.resendOtp({ email, purpose });

  return request("/auth/resend-otp", {
    method: "POST",
    auth: false,
    body: { email, purpose },
  });
}

// Password reset backend is not implemented in the current backend scope.
// Keep these mock-only so the existing UI still works in demo mode.
export async function requestPasswordReset({ email }) {
  if (MOCK_MODE) return mock.requestPasswordReset({ email });
  throw new Error("Password reset is not implemented in the current backend yet.");
}

export async function resetPassword({ email, resetToken, newPassword }) {
  if (MOCK_MODE) return mock.resetPassword({ email, resetToken, newPassword });
  throw new Error("Password reset is not implemented in the current backend yet.");
}
