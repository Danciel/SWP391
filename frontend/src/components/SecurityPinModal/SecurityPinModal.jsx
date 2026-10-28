
import { useEffect, useState } from "react";
import {
  getPinStatus,
  setupPin,
  verifyPin
} from "../../api/securityPinApi";

export default function SecurityPinModal({
  scope,
  resourceId,
  onVerified,
  onClose
}) {
  const [mode, setMode] = useState("loading");
  const [pin, setPin] = useState("");
  const [confirmPin, setConfirmPin] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  // B1. Kiểm tra đã thiết lập PIN chưa
  useEffect(() => {
    let active = true;

    getPinStatus()
      .then((result) => {
        if (active) {
          setMode(
            result.configured ? "verify" : "setup"
          );
        }
      })
      .catch((err) => {
        if (active) {
          setError(err.message || "Cannot check PIN.");
          setMode("error");
        }
      });

    return () => {
      active = false;
    };
  }, []);

  function onlyDigits(value) {
    return value.replace(/\D/g, "").slice(0, 6);
  }

  // B2. Thiết lập hoặc xác minh PIN
  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    if (!/^[0-9]{6}$/.test(pin)) {
      setError("PIN must contain exactly 6 digits.");
      return;
    }

    if (mode === "setup" && pin !== confirmPin) {
      setError("PIN confirmation does not match.");
      return;
    }

    setBusy(true);

    try {
      if (mode === "setup") {
        await setupPin(pin, confirmPin);
      }

      const result = await verifyPin(
        pin,
        scope,
        resourceId
      );

      setPin("");
      setConfirmPin("");

      // Chỉ trả Grant do Backend cấp
      // Không lưu PIN vào localStorage
      await onVerified(result.grant);
    } catch (err) {
      setError(
        err.message || "PIN verification failed."
      );
      setPin("");
      setConfirmPin("");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.6)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 9999,
        padding: 16
      }}
    >
      <form
        onSubmit={handleSubmit}
        role="dialog"
        aria-modal="true"
        aria-label="Security PIN"
        style={{
          background: "#fff",
          padding: 28,
          borderRadius: 14,
          width: "100%",
          maxWidth: 420,
          display: "flex",
          flexDirection: "column",
          gap: 15
        }}
      >
        <h2 style={{ margin: 0 }}>
          {mode === "setup"
            ? "Create Security PIN"
            : "Enter Security PIN"}
        </h2>

        <p style={{ margin: 0, color: "#64748b" }}>
          {mode === "setup"
            ? "Create a 6-digit PIN to protect your assets and wills."
            : "Enter your 6-digit PIN to unlock."}
        </p>

        {mode === "loading" ? (
          <p>Checking PIN...</p>
        ) : mode !== "error" ? (
          <>
            <label>
              Security PIN
              <input
                type="password"
                inputMode="numeric"
                autoComplete="off"
                maxLength={6}
                value={pin}
                onChange={(e) =>
                  setPin(onlyDigits(e.target.value))
                }
                placeholder="••••••"
                required
                style={{
                  width: "100%",
                  padding: 12,
                  boxSizing: "border-box",
                  borderRadius: 8,
                  border: "1px solid #cbd5e1"
                }}
              />
            </label>

            {mode === "setup" && (
              <label>
                Confirm PIN
                <input
                  type="password"
                  inputMode="numeric"
                  autoComplete="off"
                  maxLength={6}
                  value={confirmPin}
                  onChange={(e) =>
                    setConfirmPin(
                      onlyDigits(e.target.value)
                    )
                  }
                  placeholder="••••••"
                  required
                  style={{
                    width: "100%",
                    padding: 12,
                    boxSizing: "border-box",
                    borderRadius: 8,
                    border: "1px solid #cbd5e1"
                  }}
                />
              </label>
            )}
          </>
        ) : null}

        {error && (
          <p role="alert" style={{ color: "#dc2626" }}>
            {error}
          </p>
        )}

        
<div
  style={{
    display: "flex",
    justifyContent: "flex-end",
    alignItems: "center",
    gap: 12,
    marginTop: 10,
  }}
>
  {/* Cancel */}
  <button
    type="button"
    onClick={onClose}
    disabled={busy}
    style={{
      minHeight: 42,
      padding: "10px 20px",
      borderRadius: 8,
      border: "1px solid #CBD5D1",
      backgroundColor: "#FFFFFF",
      color: "#40534C",
      fontSize: 14,
      fontWeight: 600,
      fontFamily: "inherit",
      cursor: busy ? "not-allowed" : "pointer",
      transition: "all 0.2s ease",
    }}
  >
    Cancel
  </button>

  {/* Create / Unlock */}
  <button
    type="submit"
    disabled={
      busy ||
      pin.length !== 6 ||
      (mode === "setup" && confirmPin.length !== 6) ||
      mode === "loading" ||
      mode === "error"
    }
    style={{
      minHeight: 42,
      padding: "10px 20px",
      borderRadius: 8,
      border: "1px solid #205E50",
      backgroundColor: "#205E50",
      color: "#FFFFFF",
      fontSize: 14,
      fontWeight: 600,
      fontFamily: "inherit",
      cursor: busy ? "not-allowed" : "pointer",
      opacity: busy ? 0.65 : 1,
      transition: "all 0.2s ease",
    }}
  >
    {busy
      ? "Verifying..."
      : mode === "setup"
        ? "Create & Unlock"
        : "Unlock"}
  </button>
</div>

      </form>
    </div>
  );
}
