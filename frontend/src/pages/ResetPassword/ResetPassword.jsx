import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import FormField from "../../components/FormField/FormField";
import { useAuth } from "../../context/AuthContext";
import { validatePassword, validateConfirmPassword, runValidators } from "../../utils/validation";
import { ApiError } from "../../api/client";
import logo from "../../assets/logo.png";
import AuthShell from "../../components/AuthShell/AuthShell";
import styles from "./ResetPassword.module.css";

const initialForm = { newPassword: "", confirmPassword: "" };

export default function ResetPassword() {
  const { resetPassword } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const email = location.state?.email || "";
  const resetToken = location.state?.resetToken || "";

  const [form, setForm] = useState(initialForm);
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!email || !resetToken) {
      navigate("/forgot-password", { replace: true });
    }
  }, [email, resetToken, navigate]);

  const fieldValidators = {
    newPassword: () => validatePassword(form.newPassword),
    confirmPassword: () => validateConfirmPassword(form.newPassword, form.confirmPassword),
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

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitError("");

    const { isValid, errors: fieldErrors } = runValidators(fieldValidators);
    setErrors(fieldErrors);
    if (!isValid) return;

    setIsSubmitting(true);
    try {
      await resetPassword({ email, resetToken, newPassword: form.newPassword });
      navigate("/login", { state: { registeredEmail: email } });
    } catch (err) {
      setSubmitError(
        err instanceof ApiError ? err.message : "Could not reset password, please try again"
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthShell>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <img src={logo} alt="LegacyVault" className={styles.logo} />
        <h1 className={styles.title}>Reset Password</h1>
        <p className={styles.subtitle}>Create a new password for your account</p>

        <FormField
          label="New Password"
          type="password"
          name="newPassword"
          value={form.newPassword}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.newPassword}
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

        {submitError && <p className={styles.submitError}>{submitError}</p>}

        <button type="submit" className={styles.submitBtn} disabled={isSubmitting}>
          {isSubmitting ? "Updating..." : "Update Password"}
        </button>
      </form>
    </AuthShell>
  );
}
