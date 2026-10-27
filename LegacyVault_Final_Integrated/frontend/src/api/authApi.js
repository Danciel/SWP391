import { request, setToken } from "./client";
export const registerOwner = ({ fullName, email, phoneNumber, password }) => request("/auth/register", { method:"POST", auth:false, body:{fullName,email,phoneNumber,password} });
export async function login({email,password,rememberMe}) { const data=await request("/auth/login",{method:"POST",auth:false,body:{email,password,rememberMe:!!rememberMe}});setToken(data.token);return data; }
export const logout=()=>setToken(null);
export const verifyOtp=({email,otp,purpose="register"})=>request("/auth/verify-otp",{method:"POST",auth:false,body:{email,otp,purpose}});
export const resendOtp=({email,purpose="register"})=>request("/auth/resend-otp",{method:"POST",auth:false,body:{email,purpose}});
import { unsupported } from "./unsupported";
// export const requestPasswordReset=unsupported("Password reset");
// export const resetPassword=unsupported("Password reset");

// Đổi mật khẩu khi đang đăng nhập (cần mật khẩu hiện tại)
export const changePassword = ({ currentPassword, newPassword }) =>
  request("/auth/change-password", { method: "POST", body: { currentPassword, newPassword } });
