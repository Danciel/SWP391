import { request } from "./client";

// ==========================================
// PHONE OTP ENDPOINTS (Requires logged-in user)
// ==========================================

/**
 * Triggers the backend to send an OTP to the currently logged-in user's phone number.
 * Note: The backend extracts the phone number from the JWT, so no body is needed.
 */
export const sendPhoneOtp = () =>
    request("/auth/send-phone-otp", {
        method: "POST"
    });

/**
 * Verifies the OTP sent to the user's phone.
 * @param {string} otp - The 6-digit code (e.g., "123456")
 */
export const verifyPhoneOtp = (otp) =>
    request("/auth/verify-phone-otp", {
        method: "POST",
        body: { otp }
    });