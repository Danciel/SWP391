import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import AuthShell from "../../components/AuthShell/AuthShell";
import styles from "./VerifyOtp.module.css";

const OTP_LENGTH = 6;
const RESEND_SECONDS = 45;

export default function VerifyOtp() {
  const { verifyOtp, resendOtp } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const email = location.state?.email || "";
  const purpose = location.state?.purpose || "register";

  const [digits, setDigits] = useState(Array(OTP_LENGTH).fill(""));
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(RESEND_SECONDS);
  const inputRefs = useRef([]);

  useEffect(() => {
    if (!email) {
      navigate(purpose === "reset" ? "/forgot-password" : "/register", { replace: true });
    }
  }, [email, purpose, navigate]);

  useEffect(() => {
    if (secondsLeft <= 0) return;
    const timer = setInterval(() => setSecondsLeft((s) => s - 1), 1000);
    return () => clearInterval(timer);
  }, [secondsLeft]);

  function handleDigitChange(index, value) {
    const clean = value.replace(/\D/g, "").slice(-1);
    setDigits((prev) => {
      const next = [...prev];
      next[index] = clean;
      return next;
    });
    if (clean && index < OTP_LENGTH - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  }

  function handleKeyDown(index, e) {
    if (e.key === "Backspace" && !digits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  }

  function handlePaste(e) {
    const pasted = e.clipboardData.getData("text").replace(/\D/g, "").slice(0, OTP_LENGTH);
    if (!pasted) return;
    e.preventDefault();
    const next = Array(OTP_LENGTH).fill("");
    pasted.split("").forEach((char, i) => {
      next[i] = char;
    });
    setDigits(next);
    inputRefs.current[Math.min(pasted.length, OTP_LENGTH - 1)]?.focus();
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    const otp = digits.join("");
    if (otp.length !== OTP_LENGTH) {
      setError("Please enter all 6 digits");
      return;
    }

    setIsSubmitting(true);
    try {
      const data = await verifyOtp({ email, otp, purpose });
      if (purpose === "reset") {
        navigate("/reset-password", {
          state: { email, resetToken: data?.resetToken },
        });
      } else {
        navigate("/login", { state: { registeredEmail: email } });
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Verification failed, please try again");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleResend() {
    if (secondsLeft > 0 || isResending) return;
    setError("");
    setIsResending(true);
    try {
      await resendOtp({ email, purpose });
      setSecondsLeft(RESEND_SECONDS);
      setDigits(Array(OTP_LENGTH).fill(""));
      inputRefs.current[0]?.focus();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not resend the code, please try again");
    } finally {
      setIsResending(false);
    }
  }

  return (
    <AuthShell>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <img src={logo} alt="LegacyVault" className={styles.logo} />
        <h1 className={styles.title}>Verify your account</h1>
        <p className={styles.subtitle}>
          We sent a 6-digit code to {email ? <strong>{email}</strong> : "your email"}
        </p>

        <div className={styles.otpRow} onPaste={handlePaste}>
          {digits.map((digit, index) => (
            <input
              key={index}
              ref={(el) => (inputRefs.current[index] = el)}
              className={styles.otpInput}
              type="text"
              inputMode="numeric"
              maxLength={1}
              value={digit}
              onChange={(e) => handleDigitChange(index, e.target.value)}
              onKeyDown={(e) => handleKeyDown(index, e)}
            />
          ))}
        </div>

        {error && <p className={styles.submitError}>{error}</p>}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting}>
          {isSubmitting ? "Verifying..." : "Verify"}
        </button>

        <p className={styles.resendText}>
          {secondsLeft > 0 ? (
            <>Resend code in {String(secondsLeft).padStart(2, "0")}s</>
          ) : (
            <button
              type="button"
              className={styles.resendBtn}
              onClick={handleResend}
              disabled={isResending}
            >
              {isResending ? "Resending..." : "Resend code"}
            </button>
          )}
        </p>

        <p className={styles.footerText}>
          <Link to="/login">← Back to Login</Link>
        </p>
      </form>
    </AuthShell>
  );
}
