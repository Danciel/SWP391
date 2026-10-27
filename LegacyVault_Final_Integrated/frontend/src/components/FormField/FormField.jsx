import { useState } from "react";
import styles from "./FormField.module.css";

export default function FormField({
  label,
  type = "text",
  name,
  value,
  onChange,
  onBlur,
  error,
  placeholder,
  autoComplete,
  toggleable = false,
  maxLength,
  inputMode,
}) {
  const [visible, setVisible] = useState(false);
  const isPassword = type === "password";
  const resolvedType = isPassword && toggleable ? (visible ? "text" : "password") : type;

  return (
    <div className={styles.field}>
      <label htmlFor={name} className={styles.label}>
        {label}
      </label>
      <div className={styles.inputWrapper}>
        <input
          id={name}
          name={name}
          type={resolvedType}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          placeholder={placeholder}
          autoComplete={autoComplete}
          maxLength={maxLength}
          inputMode={inputMode}
          className={error ? `${styles.input} ${styles.inputError}` : styles.input}
        />
        {isPassword && toggleable && (
          <button
            type="button"
            className={styles.toggleBtn}
            onClick={() => setVisible((v) => !v)}
            tabIndex={-1}
            aria-label={visible ? "Hide password" : "Show password"}
          >
            {visible ? "🙈" : "👁"}
          </button>
        )}
      </div>
      {error && <p className={styles.error}>{error}</p>}
    </div>
  );
}
