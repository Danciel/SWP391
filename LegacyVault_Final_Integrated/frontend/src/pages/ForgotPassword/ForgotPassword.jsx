import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import FormField from "../../components/FormField/FormField";
import { useAuth } from "../../context/AuthContext";
import { validateEmail } from "../../utils/validation";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import styles from "./ForgotPassword.module.css";

export default function ForgotPassword() {
  const { requestPasswordReset } = useAuth();
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [submitError, setSubmitError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleChange(e) {
    setEmail(e.target.value);
  }

  function handleBlur() {
    setError(validateEmail(email));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitError("");

    const message = validateEmail(email);
    setError(message);
    if (message) return;

    setIsSubmitting(true);
    try {
      await requestPasswordReset({ email });
      navigate("/verify-otp", { state: { email, purpose: "reset" } });
    } catch (err) {
      setSubmitError(
        err instanceof ApiError ? err.message : "Could not send verification code, please try again"
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <img src={logo} alt="LegacyVault" className={styles.logo} />
        <h1 className={styles.title}>Forgot Password?</h1>
        <p className={styles.subtitle}>Enter your registered email to receive a verification code</p>

        <FormField
          label="Email"
          type="email"
          name="email"
          value={email}
          onChange={handleChange}
          onBlur={handleBlur}
          error={error}
          placeholder="you@example.com"
          autoComplete="email"
        />

        {submitError && <p className={styles.submitError}>{submitError}</p>}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting}>
          {isSubmitting ? "Sending..." : "Send Verification Code"}
        </button>

        <p className={styles.footerText}>
          <Link to="/login">← Back to Login</Link>
        </p>
      </form>
    </div>
  );
}
