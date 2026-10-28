
import { useEffect, useState } from "react";
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
import AuthShell from "../../components/AuthShell/AuthShell";
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
  const [isTermsOpen, setIsTermsOpen] = useState(false);

  useEffect(() => {
    if (!isTermsOpen) return;

    function handleEscape(e) {
      if (e.key === "Escape") {
        setIsTermsOpen(false);
      }
    }

    window.addEventListener("keydown", handleEscape);

    return () => {
      window.removeEventListener("keydown", handleEscape);
    };
  }, [isTermsOpen]);

  const fieldValidators = {
    fullName: () => validateFullName(form.fullName),
    email: () => validateEmail(form.email),
    phoneNumber: () => validatePhone(form.phoneNumber),
    password: () => validatePassword(form.password),
    confirmPassword: () =>
      validateConfirmPassword(form.password, form.confirmPassword),
    agreeTerms: () => validateTerms(agreeTerms),
  };

  function handleChange(e) {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function handleBlur(e) {
    const { name } = e.target;
    const message = fieldValidators[name]
      ? fieldValidators[name]()
      : "";

    setErrors((prev) => ({ ...prev, [name]: message }));
  }

  function handleTermsChange(e) {
    const checked = e.target.checked;

    setAgreeTerms(checked);
    setErrors((prev) => ({
      ...prev,
      agreeTerms: validateTerms(checked),
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitError("");

    const { isValid, errors: fieldErrors } =
      runValidators(fieldValidators);

    setErrors(fieldErrors);
    if (!isValid) return;

    setIsSubmitting(true);

    try {
      await register(form);

      navigate("/verify-otp", {
        state: {
          email: form.email,
          purpose: "register",
        },
      });
    } catch (err) {
      setSubmitError(
        err instanceof ApiError
          ? err.message
          : "Registration failed, please try again"
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthShell>
      <form
        className={styles.card}
        onSubmit={handleSubmit}
        noValidate
      >
        <img
          src={logo}
          alt="LegacyVault"
          className={styles.logo}
        />

        <h1 className={styles.title}>
          Create your Vault Owner account
        </h1>

        <p className={styles.subtitle}>
          Start protecting your digital legacy
        </p>

        <FormField
          label="Full Name"
          name="fullName"
          value={form.fullName}
          onChange={handleChange}
          onBlur={handleBlur}
          error={errors.fullName}
          placeholder="ex: Nguyễn Văn A"
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

        {/* Terms checkbox and link */}
        <div className={styles.termsRow}>
          <div className={styles.checkboxLabel}>
            <input
              id="agreeTerms"
              type="checkbox"
              checked={agreeTerms}
              onChange={handleTermsChange}
            />

            <label htmlFor="agreeTerms">
              I agree to the
            </label>

            <button
              type="button"
              className={styles.termsLink}
              onClick={() => setIsTermsOpen(true)}
              aria-haspopup="dialog"
            >
              Terms &amp; Privacy Policy
            </button>
          </div>

          {errors.agreeTerms && (
            <p className={styles.error}>
              {errors.agreeTerms}
            </p>
          )}
        </div>

        {submitError && (
          <p className={styles.submitError}>
            {submitError}
          </p>
        )}

        <button
          type="submit"
          className={styles.submitBtn}
          disabled={isSubmitting}
        >
          {isSubmitting
            ? "Creating account..."
            : "Create Account"}
        </button>

        <p className={styles.footerText}>
          Already have an account?{" "}
          <Link to="/login">Sign In</Link>
        </p>
      </form>

      {/* Terms and Privacy modal */}
      {isTermsOpen && (
        <div
          className={styles.modalOverlay}
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) {
              setIsTermsOpen(false);
            }
          }}
        >
          <section
            className={styles.modal}
            role="dialog"
            aria-modal="true"
            aria-labelledby="termsTitle"
          >
            <header className={styles.modalHeader}>
              <div>
                <h2
                  id="termsTitle"
                  className={styles.modalTitle}
                >
                  Terms &amp; Privacy Policy
                </h2>

                <p className={styles.modalSubtitle}>
                  LegacyVault Account Registration
                </p>
              </div>

              <button
                type="button"
                className={styles.closeButton}
                onClick={() => setIsTermsOpen(false)}
                aria-label="Close terms and privacy policy"
              >
                &times;
              </button>
            </header>

            <div className={styles.termsContent}>
              <section>
                <h3>1. Acceptance of Terms</h3>
                <p>
                  By creating a LegacyVault account, you
                  acknowledge that you have read and agree
                  to the applicable terms governing your
                  use of the platform.
                </p>
              </section>

              <section>
                <h3>2. Account Registration</h3>
                <p>
                  You must provide accurate and up-to-date
                  information when registering. You are
                  responsible for maintaining the
                  confidentiality of your login credentials
                  and for activities performed through
                  your account.
                </p>
              </section>

              <section>
                <h3>3. Digital Asset Management</h3>
                <p>
                  LegacyVault provides tools for managing
                  digital assets, wills, beneficiaries and
                  related inheritance instructions. You
                  should only submit information and
                  instructions that you are authorized
                  to provide.
                </p>
              </section>

              <section>
                <h3>4. Beneficiaries and Executors</h3>
                <p>
                  You are responsible for providing
                  accurate beneficiary and executor
                  information and for reviewing your
                  inheritance instructions before
                  confirming them.
                </p>
              </section>

              <section>
                <h3>5. Privacy and Personal Information</h3>
                <p>
                  Personal information submitted to
                  LegacyVault is handled in accordance
                  with the platform's applicable privacy
                  practices and policies. Access to
                  sensitive information should be limited
                  to authorized purposes.
                </p>
              </section>

              <section>
                <h3>6. Security</h3>
                <p>
                  You should protect your credentials,
                  keep your contact information current
                  and promptly report suspected
                  unauthorized access.
                </p>
              </section>

              <section>
                <h3>7. Verification and Inheritance</h3>
                <p>
                  Identity checks, legal document
                  verification and inheritance handover
                  may be required for relevant features.
                  Submitting a document does not
                  necessarily mean that a verification
                  or handover has been approved.
                </p>
              </section>

              <section>
                <h3>8. Acceptable Use</h3>
                <p>
                  You must not use the platform to submit
                  fraudulent information, access data
                  without authorization or interfere with
                  the security and operation of the service.
                </p>
              </section>

              <section>
                <h3>9. Policy Updates</h3>
                <p>
                  Applicable terms and privacy practices
                  may be updated when necessary. Any
                  required notice or renewed consent
                  should be provided in accordance with
                  the applicable policy.
                </p>
              </section>

              <section>
                <h3>10. Contact and Questions</h3>
                <p>
                  If you have questions about these terms
                  or the handling of your personal
                  information, contact the platform's
                  designated support channel.
                </p>
              </section>
            </div>

            <footer className={styles.modalFooter}>
              <button
                type="button"
                className={styles.closeModalBtn}
                onClick={() => setIsTermsOpen(false)}
              >
                Close
              </button>
            </footer>
          </section>
        </div>
      )}
    </AuthShell>
  );
}
