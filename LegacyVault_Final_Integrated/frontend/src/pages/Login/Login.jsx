import { useEffect, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import FormField from "../../components/FormField/FormField";
import { useAuth } from "../../context/AuthContext";
import { validateEmail, validatePassword, runValidators } from "../../utils/validation";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import styles from "./Login.module.css";

const initialForm = { email: "", password: "" };

export default function Login() {
  const { login, sessionNotice, clearSessionNotice } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const registeredEmail = location.state?.registeredEmail || "";

  const [form, setForm] = useState({ ...initialForm, email: registeredEmail });
  const [rememberMe, setRememberMe] = useState(false);
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [cooldown, setCooldown] = useState(0); // N4: seconds left after HTTP 429
  const [locked, setLocked] = useState(false); // N2: ACCOUNT_LOCKED

  useEffect(() => {
    if (cooldown <= 0) return undefined;
    const t = setTimeout(() => setCooldown((c) => c - 1), 1000);
    return () => clearTimeout(t);
  }, [cooldown]);

  function handleChange(e) {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function validateField(name, value) {
    const validators = {
      email: () => validateEmail(value),
      password: () => (value ? "" : "Please enter your password"),
    };
    const message = validators[name] ? validators[name]() : "";
    setErrors((prev) => ({ ...prev, [name]: message }));
  }

  function handleBlur(e) {
    const { name, value } = e.target;
    validateField(name, value);
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitError("");
    setLocked(false);
    if (cooldown > 0) return;

    const { isValid, errors: fieldErrors } = runValidators({
      email: () => validateEmail(form.email),
      password: () => (form.password ? "" : "Please enter your password"),
    });
    setErrors(fieldErrors);
    if (!isValid) return;

    setIsSubmitting(true);
    try {
      await login({ ...form, rememberMe });
      clearSessionNotice();
      navigate("/dashboard");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 429) {
          setCooldown(err.retryAfter || 60);
          setSubmitError("Too many failed attempts. Please wait before trying again.");
        } else if (err.code === "ACCOUNT_LOCKED" || /account.*\blocked\b/i.test(err.message)) {
          setLocked(true);
          setSubmitError(err.message);
        } else {
          const left = err.payload?.remainingAttempts;
          setSubmitError(
            left != null && left >= 0
              ? `${err.message} ${left} attempt${left === 1 ? "" : "s"} left before a temporary lock.`
              : err.message
          );
        }
      } else {
        setSubmitError("Login failed, please try again");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <img src={logo} alt="LegacyVault" className={styles.logo} />
        <h1 className={styles.title}>Sign in to LegacyVault</h1>
        <p className={styles.subtitle}>Access your digital legacy vault</p>

        {sessionNotice && <p className={styles.noticeBanner} role="alert">{sessionNotice}</p>}

        {registeredEmail && (
          <p className={styles.successBanner}>
            Registration successful! Please sign in to continue.
          </p>
        )}

        <FormField
          label="Email"
          type="email"
          name="email"
          value={form.email}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.email}
          placeholder="you@example.com"
          autoComplete="email"
        />

        <FormField
          label="Password"
          type="password"
          name="password"
          value={form.password}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.password}
          placeholder="••••••••"
          autoComplete="current-password"
          toggleable
        />

        <div className={styles.optionsRow}>
          <label className={styles.checkboxLabel}>
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
            />
            Remember me
          </label>
          <Link to="/forgot-password" className={styles.inlineLink}>
            Forgot password?
          </Link>
        </div>

        {submitError && (
          <p className={styles.submitError} role="alert">
            {locked && <strong>Account locked. </strong>}
            {submitError}
            {cooldown > 0 && ` Try again in ${Math.floor(cooldown / 60)}:${String(cooldown % 60).padStart(2, "0")}.`}
          </p>
        )}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting || cooldown > 0}>
          {isSubmitting ? "Signing in..." : cooldown > 0 ? `Locked for ${cooldown}s` : "Sign in"}
        </button>

        <p className={styles.footerText}>
          Don't have an account? <Link to="/register">Create account</Link>
        </p>
      </form>
    </div>
  );
}
