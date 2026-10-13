import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import FormField from "../../components/FormField/FormField";
import { useAuth } from "../../context/AuthContext";
import { runValidators } from "../../utils/validation";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import styles from "./Login.module.css";

const initialForm = { identifier: "", password: "" };

const validateIdentifier = (v) => (v.trim() ? "" : "Please enter your username, email or phone");
const validatePasswordRequired = (v) => (v ? "" : "Please enter your password");

const QUICK_LOGIN_ACCOUNTS = [
  { label: "Owner", email: "owner@test.com", password: "password123" },
  { label: "Executor", email: "executor@test.com", password: "password123" },
  { label: "Legal Verifier", email: "verifier@test.com", password: "password123" },
  { label: "Admin", email: "admin@test.com", password: "password123" },
];

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const registeredEmail = location.state?.registeredEmail || "";

  const [form, setForm] = useState({ ...initialForm, identifier: registeredEmail });
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleChange(e) {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function validateField(name, value) {
    const validators = {
      identifier: () => validateIdentifier(value),
      password: () => validatePasswordRequired(value),
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

    const { isValid, errors: fieldErrors } = runValidators({
      identifier: () => validateIdentifier(form.identifier),
      password: () => validatePasswordRequired(form.password),
    });
    setErrors(fieldErrors);
    if (!isValid) return;

    setIsSubmitting(true);
    try {
      await login({ identifier: form.identifier.trim(), email: form.identifier.trim(), password: form.password });
      navigate("/dashboard");
    } catch (err) {
      setSubmitError(err instanceof ApiError ? err.message : "Login failed, please try again");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleQuickLogin(account) {
    setSubmitError("");
    setIsSubmitting(true);
    try {
      await login({ identifier: account.email, email: account.email, password: account.password });
      navigate("/dashboard");
    } catch (err) {
      setSubmitError(err instanceof ApiError ? err.message : "Login failed, please try again");
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

        {registeredEmail && (
          <p className={styles.successBanner}>
            Registration successful! Please sign in to continue.
          </p>
        )}

        <FormField
          label="Username, email or phone"
          name="identifier"
          value={form.identifier}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.identifier}
          placeholder="username, you@example.com or +84 123 456 789"
          autoComplete="username"
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
          {/*<label className={styles.checkboxLabel}>*/}
          {/*  <input*/}
          {/*    type="checkbox"*/}
          {/*    checked={rememberMe}*/}
          {/*    onChange={(e) => setRememberMe(e.target.checked)}*/}
          {/*  />*/}
          {/*  Remember me*/}
          {/*</label>*/}
          <Link to="/forgot-password" className={styles.inlineLink}>
            Forgot password?
          </Link>
        </div>

        {submitError && <p className={styles.submitError}>{submitError}</p>}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>

        <p className={styles.footerText}>
          Don't have an account? <Link to="/register">Create account</Link>
        </p>

        <div className={styles.quickLoginDivider}>
          <span>Quick login (demo)</span>
        </div>
        <div className={styles.quickLoginRow}>
          {QUICK_LOGIN_ACCOUNTS.map((account) => (
            <button
              key={account.email}
              type="button"
              className={styles.quickLoginBtn}
              disabled={isSubmitting}
              onClick={() => handleQuickLogin(account)}
            >
              {account.label}
            </button>
          ))}
        </div>
      </form>
    </div>
  );
}
