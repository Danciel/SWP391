import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { getProfile } from "../../api/vaultApi";
import { changePassword } from "../../api/authApi";
import { requestPasswordReset, resetPassword } from "../../api/resetPasswordApi";
import { getPinStatus, setupPin, changePin, requestPinReset, resetPin } from "../../api/securityPinApi";
import styles from "./Security.module.css";

const RESEND_SECONDS = 60;

function maskEmail(email) {
  if (!email || !email.includes("@")) return "your email";
  const [name, domain] = email.split("@");
  return `${name.slice(0, 2)}${"*".repeat(Math.max(name.length - 2, 2))}@${domain}`;
}

const digitsOnly = (v) => v.replace(/\D/g, "").slice(0, 6);

function errorText(err, fallback) {
  const left = err?.payload?.remainingAttempts;
  const base = err?.message || fallback;
  return typeof left === "number" ? `${base} ${left} attempt${left === 1 ? "" : "s"} left.` : base;
}

function Field({ label, type = "password", value, onChange, autoComplete, pin = false, placeholder, hint }) {
  return (
    <label className={styles.field}>
      <span className={styles.label}>{label}</span>
      <input
        className={`${styles.input} ${pin ? styles.pinInput : ""}`}
        type={type}
        value={value}
        autoComplete={autoComplete || "off"}
        inputMode={pin ? "numeric" : undefined}
        maxLength={pin ? 6 : undefined}
        placeholder={placeholder}
        onChange={(e) => onChange(pin ? digitsOnly(e.target.value) : e.target.value)}
      />
      {hint && <span className={styles.hint}>{hint}</span>}
    </label>
  );
}

function Notice({ state }) {
  if (!state?.text) return null;
  return (
    <p role={state.kind === "error" ? "alert" : "status"} className={state.kind === "error" ? styles.error : styles.success}>
      {state.text}
    </p>
  );
}

function Tabs({ value, onChange, items }) {
  return (
    <div className={styles.tabs} role="tablist">
      {items.map((it) => (
        <button
          key={it.id}
          type="button"
          role="tab"
          aria-selected={value === it.id}
          className={value === it.id ? styles.tabActive : styles.tab}
          onClick={() => onChange(it.id)}
        >
          {it.label}
        </button>
      ))}
    </div>
  );
}

// Đếm ngược để chặn gửi lại mã quá nhanh
function useCountdown() {
  const [left, setLeft] = useState(0);
  useEffect(() => {
    if (left <= 0) return undefined;
    const t = setTimeout(() => setLeft((n) => n - 1), 1000);
    return () => clearTimeout(t);
  }, [left]);
  return [left, () => setLeft(RESEND_SECONDS)];
}

/* ------------------------------ PASSWORD ------------------------------ */

function ChangePasswordForm() {
  const [form, setForm] = useState({ current: "", next: "", confirm: "" });
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState(null);
  const set = (k) => (v) => setForm((f) => ({ ...f, [k]: v }));

  async function submit(e) {
    e.preventDefault();
    setNote(null);
    if (form.next.length < 8) return setNote({ kind: "error", text: "New password must be at least 8 characters." });
    if (form.next !== form.confirm) return setNote({ kind: "error", text: "New passwords do not match." });
    if (form.next === form.current) return setNote({ kind: "error", text: "New password must be different from the current one." });

    setBusy(true);
    try {
      await changePassword({ currentPassword: form.current, newPassword: form.next });
      setForm({ current: "", next: "", confirm: "" });
      setNote({ kind: "ok", text: "Password changed." });
    } catch (err) {
      setNote({ kind: "error", text: err.message || "Could not change password." });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <Field label="Current password" value={form.current} onChange={set("current")} autoComplete="current-password" />
      <Field label="New password" value={form.next} onChange={set("next")} autoComplete="new-password" hint="At least 8 characters." />
      <Field label="Confirm new password" value={form.confirm} onChange={set("confirm")} autoComplete="new-password" />
      <Notice state={note} />
      <div className={styles.actions}>
        <button className={styles.primaryBtn} disabled={busy || !form.current || !form.next || !form.confirm}>
          {busy ? "Saving..." : "Change password"}
        </button>
      </div>
    </form>
  );
}

function ForgotPasswordForm({ email }) {
  const [sent, setSent] = useState(false);
  const [form, setForm] = useState({ otp: "", next: "", confirm: "" });
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState(null);
  const [left, startCooldown] = useCountdown();
  const set = (k) => (v) => setForm((f) => ({ ...f, [k]: v }));

  async function sendCode() {
    setNote(null);
    setBusy(true);
    try {
      await requestPasswordReset({ email });
      setSent(true);
      startCooldown();
      setNote({ kind: "ok", text: `We sent a 6-digit code to ${maskEmail(email)}. It expires in 5 minutes.` });
    } catch (err) {
      setNote({ kind: "error", text: err.message || "Could not send the code." });
    } finally {
      setBusy(false);
    }
  }

  async function submit(e) {
    e.preventDefault();
    setNote(null);
    if (form.otp.length !== 6) return setNote({ kind: "error", text: "Enter the 6-digit code from your email." });
    if (form.next.length < 8) return setNote({ kind: "error", text: "New password must be at least 8 characters." });
    if (form.next !== form.confirm) return setNote({ kind: "error", text: "New passwords do not match." });

    setBusy(true);
    try {
      await resetPassword({ email, otp: form.otp, newPassword: form.next });
      setForm({ otp: "", next: "", confirm: "" });
      setSent(false);
      setNote({ kind: "ok", text: "Password reset. Use your new password the next time you sign in." });
    } catch (err) {
      setNote({ kind: "error", text: err.message || "Could not reset password." });
    } finally {
      setBusy(false);
    }
  }

  if (!sent) {
    return (
      <div className={styles.form}>
        <p className={styles.help}>
          Forgot your current password? We will email a verification code to <strong>{maskEmail(email)}</strong>.
        </p>
        <Notice state={note} />
        <div className={styles.actions}>
          <button type="button" className={styles.primaryBtn} onClick={sendCode} disabled={busy || !email}>
            {busy ? "Sending..." : "Send code"}
          </button>
        </div>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <Field label="Verification code" type="text" pin value={form.otp} onChange={set("otp")} placeholder="000000" />
      <Field label="New password" value={form.next} onChange={set("next")} autoComplete="new-password" hint="At least 8 characters." />
      <Field label="Confirm new password" value={form.confirm} onChange={set("confirm")} autoComplete="new-password" />
      <Notice state={note} />
      <div className={styles.actions}>
        <button className={styles.primaryBtn} disabled={busy}>{busy ? "Saving..." : "Reset password"}</button>
        <button type="button" className={styles.secondaryBtn} onClick={sendCode} disabled={busy || left > 0}>
          {left > 0 ? `Resend in ${left}s` : "Resend code"}
        </button>
      </div>
    </form>
  );
}

function PasswordSection({ email }) {
  const [tab, setTab] = useState("change");
  return (
    <section className={styles.card}>
      <h2 className={styles.sectionTitle}>Password</h2>
      <p className={styles.sectionText}>Used to sign in to your account.</p>
      <Tabs value={tab} onChange={setTab} items={[{ id: "change", label: "Change password" }, { id: "forgot", label: "Forgot password" }]} />
      {tab === "change" ? <ChangePasswordForm /> : <ForgotPasswordForm email={email} />}
    </section>
  );
}

/* -------------------------------- PIN --------------------------------- */

function SetupPinForm({ onDone }) {
  const [form, setForm] = useState({ pin: "", confirm: "" });
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState(null);
  const set = (k) => (v) => setForm((f) => ({ ...f, [k]: v }));

  async function submit(e) {
    e.preventDefault();
    setNote(null);
    if (form.pin.length !== 6) return setNote({ kind: "error", text: "PIN must be exactly 6 digits." });
    if (form.pin !== form.confirm) return setNote({ kind: "error", text: "PINs do not match." });
    setBusy(true);
    try {
      await setupPin(form.pin, form.confirm);
      onDone();
    } catch (err) {
      setNote({ kind: "error", text: err.message || "Could not create PIN." });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <p className={styles.help}>You have not created a security PIN yet. It protects your will details and asset secrets.</p>
      <Field label="New PIN" type="password" pin value={form.pin} onChange={set("pin")} placeholder="6 digits" />
      <Field label="Confirm PIN" type="password" pin value={form.confirm} onChange={set("confirm")} placeholder="6 digits" />
      <Notice state={note} />
      <div className={styles.actions}>
        <button className={styles.primaryBtn} disabled={busy}>{busy ? "Saving..." : "Create PIN"}</button>
      </div>
    </form>
  );
}

function ChangePinForm() {
  const [form, setForm] = useState({ current: "", next: "", confirm: "" });
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState(null);
  const set = (k) => (v) => setForm((f) => ({ ...f, [k]: v }));

  async function submit(e) {
    e.preventDefault();
    setNote(null);
    if (form.current.length !== 6 || form.next.length !== 6) return setNote({ kind: "error", text: "PINs must be exactly 6 digits." });
    if (form.next !== form.confirm) return setNote({ kind: "error", text: "New PINs do not match." });
    if (form.next === form.current) return setNote({ kind: "error", text: "New PIN must be different from the current one." });

    setBusy(true);
    try {
      await changePin(form.current, form.next, form.confirm);
      setForm({ current: "", next: "", confirm: "" });
      setNote({ kind: "ok", text: "PIN changed." });
    } catch (err) {
      setNote({ kind: "error", text: errorText(err, "Could not change PIN.") });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <Field label="Current PIN" pin value={form.current} onChange={set("current")} placeholder="6 digits" />
      <Field label="New PIN" pin value={form.next} onChange={set("next")} placeholder="6 digits" />
      <Field label="Confirm new PIN" pin value={form.confirm} onChange={set("confirm")} placeholder="6 digits" />
      <Notice state={note} />
      <div className={styles.actions}>
        <button className={styles.primaryBtn} disabled={busy}>{busy ? "Saving..." : "Change PIN"}</button>
      </div>
    </form>
  );
}

function ForgotPinForm({ email }) {
  const [sent, setSent] = useState(false);
  const [form, setForm] = useState({ otp: "", next: "", confirm: "" });
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState(null);
  const [left, startCooldown] = useCountdown();
  const set = (k) => (v) => setForm((f) => ({ ...f, [k]: v }));

  async function sendCode() {
    setNote(null);
    setBusy(true);
    try {
      await requestPinReset();
      setSent(true);
      startCooldown();
      setNote({ kind: "ok", text: `We sent a 6-digit code to ${maskEmail(email)}. It expires in 5 minutes.` });
    } catch (err) {
      setNote({ kind: "error", text: err.message || "Could not send the code." });
    } finally {
      setBusy(false);
    }
  }

  async function submit(e) {
    e.preventDefault();
    setNote(null);
    if (form.otp.length !== 6) return setNote({ kind: "error", text: "Enter the 6-digit code from your email." });
    if (form.next.length !== 6) return setNote({ kind: "error", text: "PIN must be exactly 6 digits." });
    if (form.next !== form.confirm) return setNote({ kind: "error", text: "New PINs do not match." });

    setBusy(true);
    try {
      await resetPin(form.otp, form.next, form.confirm);
      setForm({ otp: "", next: "", confirm: "" });
      setSent(false);
      setNote({ kind: "ok", text: "PIN reset. Use your new PIN to open protected details." });
    } catch (err) {
      setNote({ kind: "error", text: err.message || "Could not reset PIN." });
    } finally {
      setBusy(false);
    }
  }

  if (!sent) {
    return (
      <div className={styles.form}>
        <p className={styles.help}>
          Forgot your PIN? We will email a verification code to <strong>{maskEmail(email)}</strong>, then you can set a new PIN.
        </p>
        <Notice state={note} />
        <div className={styles.actions}>
          <button type="button" className={styles.primaryBtn} onClick={sendCode} disabled={busy || !email}>
            {busy ? "Sending..." : "Send code"}
          </button>
        </div>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={submit}>
      <Field label="Verification code" type="text" pin value={form.otp} onChange={set("otp")} placeholder="000000" />
      <Field label="New PIN" pin value={form.next} onChange={set("next")} placeholder="6 digits" />
      <Field label="Confirm new PIN" pin value={form.confirm} onChange={set("confirm")} placeholder="6 digits" />
      <Notice state={note} />
      <div className={styles.actions}>
        <button className={styles.primaryBtn} disabled={busy}>{busy ? "Saving..." : "Reset PIN"}</button>
        <button type="button" className={styles.secondaryBtn} onClick={sendCode} disabled={busy || left > 0}>
          {left > 0 ? `Resend in ${left}s` : "Resend code"}
        </button>
      </div>
    </form>
  );
}

function PinSection({ email }) {
  const [configured, setConfigured] = useState(null); // null = đang tải
  const [loadError, setLoadError] = useState("");
  const [tab, setTab] = useState("change");

  async function loadStatus() {
    setLoadError("");
    try {
      const s = await getPinStatus();
      setConfigured(!!s.configured);
    } catch (err) {
      setLoadError(err.message || "Could not load PIN status.");
    }
  }

  useEffect(() => {
    loadStatus();
  }, []);

  return (
    <section className={styles.card}>
      <h2 className={styles.sectionTitle}>Security PIN</h2>
      <p className={styles.sectionText}>A 6-digit PIN that protects your will details and asset secrets.</p>
      {loadError ? (
        <p role="alert" className={styles.error}>{loadError}</p>
      ) : configured === null ? (
        <p className={styles.help}>Loading...</p>
      ) : !configured ? (
        <SetupPinForm onDone={() => setConfigured(true)} />
      ) : (
        <>
          <Tabs value={tab} onChange={setTab} items={[{ id: "change", label: "Change PIN" }, { id: "forgot", label: "Forgot PIN" }]} />
          {tab === "change" ? <ChangePinForm /> : <ForgotPinForm email={email} />}
        </>
      )}
    </section>
  );
}

/* -------------------------------- PAGE -------------------------------- */

export default function Security() {
  const { user } = useAuth();
  const [email, setEmail] = useState("");
  // PIN bảo vệ chi tiết Will/Asset nên chỉ dành cho Owner.
  const hasPin = user?.role === "OWNER" || user?.role === "USER";

  useEffect(() => {
    let cancelled = false;
    getProfile()
      .then((p) => !cancelled && setEmail(p.email || ""))
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Security</h1>
      <PasswordSection email={email} />
      {hasPin && <PinSection email={email} />}
    </div>
  );
}
