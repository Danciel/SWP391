import { createContext, useContext, useState, useCallback, useEffect } from "react";
import {
  getToken,
  setToken as persistToken,
  getStoredUser,
  setStoredUser,
  setRefreshToken,
  request,
  SESSION_ENDED_EVENT,
} from "../api/client";
import {
  login as loginApi,
  registerOwner as registerApi,
  logout as logoutApi,
  verifyOtp as verifyOtpApi,
  resendOtp as resendOtpApi,
} from "../api/authApi";
import {
  requestPasswordReset as requestPasswordResetApi,
  resetPassword as resetPasswordApi,
} from "../api/resetPasswordApi";

const AuthContext = createContext(null);

// reason -> message shown on the login page
const SESSION_NOTICE = {
  locked: "Your account has been locked, so you were signed out. Please contact support if you think this is a mistake.",
  expired: "Your session has expired. Please sign in again.",
};

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => getStoredUser());
  const [isAuthenticated, setIsAuthenticated] = useState(!!getToken());
  const [sessionNotice, setSessionNotice] = useState("");

  const login = useCallback(async (credentials) => {
    const data = await loginApi(credentials);
    setSessionNotice("");
    setUser(data.user);
    setStoredUser(data.user);
    setIsAuthenticated(true);
    return data;
  }, []);

  const register = useCallback(async (form) => {
    const data = await registerApi(form);
    return data;
  }, []);

  const clearLocalSession = useCallback(() => {
    persistToken(null);
    setRefreshToken(null);
    setStoredUser(null);
    setUser(null);
    setIsAuthenticated(false);
  }, []);

  const logout = useCallback(() => {
    logoutApi(); // revoke refresh token on the server (fire and forget)
    clearLocalSession();
  }, [clearLocalSession]);

  // N2/N5: request() fires this when the account is locked or the token cannot be refreshed.
  useEffect(() => {
    function onSessionEnded(e) {
      clearLocalSession();
      // A stale token found on app start is dropped silently; only a session that ends while in use is announced.
      if (!e.detail?.quiet) setSessionNotice(SESSION_NOTICE[e.detail?.reason] || SESSION_NOTICE.expired);
    }
    window.addEventListener(SESSION_ENDED_EVENT, onSessionEnded);
    return () => window.removeEventListener(SESSION_ENDED_EVENT, onSessionEnded);
  }, [clearLocalSession]);

  // On app start, validate the stored JWT once. A locked account / revoked token ends the session
  // here instead of waiting for the first random API call to fail.
  useEffect(() => {
    if (!getToken()) return;
    request("/accounts/me", { quietExpiry: true })
      .then((me) => {
        setUser((prev) => {
          const merged = { ...(prev || {}), ...me };
          setStoredUser(merged);
          return merged;
        });
      })
      .catch(() => {}); // 401 / ACCOUNT_LOCKED are handled inside request()
  }, []);

  const clearSessionNotice = useCallback(() => setSessionNotice(""), []);

  const verifyOtp = useCallback(async (payload) => verifyOtpApi(payload), []);
  const resendOtp = useCallback(async (payload) => resendOtpApi(payload), []);
  const requestPasswordReset = useCallback(async (payload) => requestPasswordResetApi(payload), []);
  const resetPassword = useCallback(async (payload) => resetPasswordApi(payload), []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        sessionNotice,
        clearSessionNotice,
        login,
        register,
        logout,
        verifyOtp,
        resendOtp,
        requestPasswordReset,
        resetPassword,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an <AuthProvider>");
  return ctx;
}
