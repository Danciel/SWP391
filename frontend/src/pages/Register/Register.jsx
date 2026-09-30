import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import FormField from "../../components/FormField/FormField";
import { useAuth } from "../../context/AuthContext";
import {
  validateFullName,
  validateEmail,
  validatePhone,
  validatePassword,
  validateConfirmPassword,
  validateTerms,
  runValidators,
} from "../../utils/validation";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import styles from "./Register.module.css";

const initialForm = {
  fullName: "",
  email: "",
  phoneNumber: "",
  password: "",
  confirmPassword: "",
};

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState(initialForm);
  const [agreeTerms, setAgreeTerms] = useState(false);
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fieldValidators = {
    fullName: () => validateFullName(form.fullName),
    email: () => validateEmail(form.email),
    phoneNumber: () => validatePhone(form.phoneNumber),
    password: () => validatePassword(form.password),
    confirmPassword: () => validateConfirmPassword(form.password, form.confirmPassword),
    agreeTerms: () => validateTerms(agreeTerms),
  };

  function handleChange(e) {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function handleBlur(e) {
    const { name } = e.target;
    const message = fieldValidators[name] ? fieldValidators[name]() : "";
    setErrors((prev) => ({ ...prev, [name]: message }));
  }

  function handleTermsChange(e) {
    const checked = e.target.checked;
    setAgreeTerms(checked);
    setErrors((prev) => ({ ...prev, agreeTerms: validateTerms(checked) }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitError("");

    const { isValid, errors: fieldErrors } = runValidators(fieldValidators);
    setErrors(fieldErrors);
    if (!isValid) return;

    setIsSubmitting(true);
    try {
      await register(form);
      navigate("/verify-otp", {
        state: { email: form.email, purpose: "register" },
      });
    } catch (err) {
      setSubmitError(err instanceof ApiError ? err.message : "Registration failed, please try again");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <img src={logo} alt="LegacyVault" className={styles.logo} />
        <h1 className={styles.title}>Create your Vault Owner account</h1>
        <p className={styles.subtitle}>Start protecting your digital legacy</p>

        <FormField
          label="Full Name"
          name="fullName"
          value={form.fullName}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.fullName}
          placeholder="John Doe"
          autoComplete="name"
        />

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
          label="Phone Number"
          type="tel"
          name="phoneNumber"
          value={form.phoneNumber}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.phoneNumber}
          placeholder="+84 123 456 789"
          autoComplete="tel"
        />

        <FormField
          label="Password"
          type="password"
          name="password"
          value={form.password}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.password}
          placeholder="At least 8 characters, upper/lowercase & a number"
          autoComplete="new-password"
          toggleable
        />

        <FormField
          label="Confirm Password"
          type="password"
          name="confirmPassword"
          value={form.confirmPassword}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.confirmPassword}
          placeholder="••••••••"
          autoComplete="new-password"
          toggleable
        />

        <div className={styles.termsRow}>
          <label className={styles.checkboxLabel}>
            <input type="checkbox" checked={agreeTerms} onChange={handleTermsChange} />
            I agree to the Terms & Privacy Policy
          </label>
          {errors.agreeTerms && <p className={styles.error}>{errors.agreeTerms}</p>}
        </div>

        {submitError && <p className={styles.submitError}>{submitError}</p>}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting}>
          {isSubmitting ? "Creating account..." : "Create Account"}
        </button>

        <p className={styles.footerText}>
          Already have an account? <Link to="/login">Sign In</Link>
        </p>
      </form>
    </div>
  );
}
