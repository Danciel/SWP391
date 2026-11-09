const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PASSWORD_RE = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;
const PHONE_RE = /^\d{10}$/;
const IDENTIFY_NUMBER_RE = /^\d{12}$/;
const OTP_RE = /^\d{6}$/;

export function validateFullName(value) {
  if (!value || !value.trim()) return "Please enter your full name";
  if (value.trim().length < 2) return "Full name is too short";
  return "";
}

export function validateEmail(value) {
  if (!value || !value.trim()) return "Please enter your email";
  if (!EMAIL_RE.test(value.trim())) return "Invalid email address";
  return "";
}

export function validatePassword(value) {
  if (!value) return "Please enter a password";
  if (!PASSWORD_RE.test(value)) {
    return "Password must be at least 8 characters and include uppercase, lowercase, and a number";
  }
  return "";
}

export function validateConfirmPassword(password, confirm) {
  if (!confirm) return "Please confirm your password";
  if (password !== confirm) return "Passwords do not match";
  return "";
}

export function validatePhone(value) {
  if (!value || !value.trim()) return "Please enter your phone number";
  if (!PHONE_RE.test(value.trim())) return "Phone number must be exactly 10 digits";
  return "";
}

export function validateIdentifyNumber(value) {
  if (!value || !value.trim()) return "Please enter your identity number";
  if (!IDENTIFY_NUMBER_RE.test(value.trim())) return "Identity number must be exactly 12 digits";
  return "";
}

export function validateTerms(checked) {
  if (!checked) return "You must agree to the Terms & Privacy Policy";
  return "";
}

export function validateOtp(value) {
  if (!value || !value.trim()) return "Please enter the OTP code";
  if (!OTP_RE.test(value.trim())) return "OTP must be 6 digits";
  return "";
}

export function runValidators(fieldValidators) {
  const errors = {};
  Object.entries(fieldValidators).forEach(([field, validatorFn]) => {
    const message = validatorFn();
    if (message) errors[field] = message;
  });
  return { isValid: Object.keys(errors).length === 0, errors };
}
