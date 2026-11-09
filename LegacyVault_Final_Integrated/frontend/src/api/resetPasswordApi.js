import { request } from "./client";

/**
 * Initiates the password reset flow by sending an OTP to the user's email.
 * @param {string} email - The email address of the account.
 */
export const requestPasswordReset = ({ email }) =>
    request("/auth/forgot-password", {
        method: "POST",
        auth: false,
        body: { email }
    });

/**
 * Completes the password reset flow using the OTP and the new password.
 * @param {string} email - The email address of the account.
 * @param {string} otp - The 6-digit OTP received via email.
 * @param {string} newPassword - The new password to set.
 */
export const resetPassword = ({ email, otp, newPassword }) =>
    request("/auth/reset-password", {
        method: "POST",
        auth: false,
        body: { email, otp, newPassword }
    });