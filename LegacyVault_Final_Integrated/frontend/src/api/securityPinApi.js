
import { request } from "./client";

// B1. Kiểm tra Owner đã có PIN chưa
export const getPinStatus = async () => {
  return request("/owner-pin/status");
};

// B2. Thiết lập PIN lần đầu
export const setupPin = async (pin, confirmPin) => {
  return request("/owner-pin/setup", {
    method: "POST",
    body: {
      pin,
      confirmPin
    }
  });
};

// B3. Xác minh PIN
export const verifyPin = async (
  pin,
  scope,
  resourceId
) => {
  return request("/owner-pin/verify", {
    method: "POST",
    body: {
      pin,
      scope,
      resourceId
    }
  });
};


// B4. Đổi PIN (cần PIN hiện tại)
export const changePin = async (currentPin, newPin, confirmPin) => {
  return request("/owner-pin/change", {
    method: "POST",
    body: { currentPin, newPin, confirmPin }
  });
};

// B5. Quên PIN: gửi mã OTP tới email của tài khoản đang đăng nhập
export const requestPinReset = async () => {
  return request("/owner-pin/forgot", { method: "POST" });
};

// B6. Đặt lại PIN bằng mã OTP email
export const resetPin = async (otp, newPin, confirmPin) => {
  return request("/owner-pin/reset", {
    method: "POST",
    body: { otp, newPin, confirmPin }
  });
};
