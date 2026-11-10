import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import FormField from "../../components/FormField/FormField";
import { useAuth } from "../../context/AuthContext";
import { validateEmail, validatePassword, runValidators } from "../../utils/validation";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import styles from "./Login.module.css";

const initialForm = { email: "", password: "" };

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const registeredEmail = location.state?.registeredEmail || "";

  const [form, setForm] = useState({ ...initialForm, email: registeredEmail });
  const [rememberMe, setRememberMe] = useState(false);
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

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

    const { isValid, errors: fieldErrors } = runValidators({
      email: () => validateEmail(form.email),
      password: () => (form.password ? "" : "Please enter your password"),
    });
    setErrors(fieldErrors);
    if (!isValid) return;

    setIsSubmitting(true);
    try {
      await login({ ...form, rememberMe });
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

        {submitError && <p className={styles.submitError}>{submitError}</p>}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>

        <p className={styles.footerText}>
          Don't have an account? <Link to="/register">Create account</Link>
        </p>
      </form>
    </div>
  );
}
