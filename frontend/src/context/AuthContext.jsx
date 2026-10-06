import { createContext, useContext, useState, useCallback } from "react";
import { getToken, setToken as persistToken, getStoredUser, setStoredUser } from "../api/client";
import {
  login as loginApi,
  registerOwner as registerApi,
  logout as logoutApi,
  verifyOtp as verifyOtpApi,
  resendOtp as resendOtpApi,
  requestPasswordReset as requestPasswordResetApi,
  resetPassword as resetPasswordApi,
} from "../api/authApi";

const AuthContext = createContext(null);
const USER_KEY = "lv_user";
function getStoredUser() {
  try {
    const savedUser = localStorage.getItem(USER_KEY);
    return savedUser ? JSON.parse(savedUser) : null;
  } catch {
    return null;
  }
}
export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => getStoredUser());
  const [isAuthenticated, setIsAuthenticated] = useState(!!getToken());

  const login = useCallback(async (credentials) => {
    const data = await loginApi(credentials);
    setUser(data.user);
    setStoredUser(data.user);
    localStorage.setItem(USER_KEY, JSON.stringify(data.user));
    setIsAuthenticated(true);
    return data;
  }, []);

  const register = useCallback(async (form) => {
    const data = await registerApi(form);
    return data;
  }, []);

  const logout = useCallback(() => {
    logoutApi();
    persistToken(null);
    setStoredUser(null);
    localStorage.removeItem(USER_KEY);
    setUser(null);
    setIsAuthenticated(false);
  }, []);

  const verifyOtp = useCallback(async (payload) => verifyOtpApi(payload), []);
  const resendOtp = useCallback(async (payload) => resendOtpApi(payload), []);
  const requestPasswordReset = useCallback(async (payload) => requestPasswordResetApi(payload), []);
  const resetPassword = useCallback(async (payload) => resetPasswordApi(payload), []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
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
