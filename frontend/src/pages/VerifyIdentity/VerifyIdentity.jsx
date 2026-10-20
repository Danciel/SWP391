import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { sendPhoneOtp, verifyPhoneOtp, verifyIdentityNumber, verifyFace, getProfile } from "../../api/vaultApi";
import { ApiError } from "../../api/client";
import { validateIdentifyNumber } from "../../utils/validation";
import styles from "./VerifyIdentity.module.css";

const STEPS = ["Phone", "Identity", "Face"];

export default function VerifyIdentity() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnTo = params.get("returnTo") || "/dashboard/profile";

  const [stepIndex, setStepIndex] = useState(0);
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [otpSent, setOtpSent] = useState(false);
  const [phoneDone, setPhoneDone] = useState(false);
  const [identityNumber, setIdentityNumber] = useState("");
  const [identityDone, setIdentityDone] = useState(false);
  const [identityError, setIdentityError] = useState("");
  const [scanning, setScanning] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [finalProfile, setFinalProfile] = useState(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function handleSendOtp() {
    setError("");
    setBusy(true);
    try {
      await sendPhoneOtp(user?.id, phone);
      setOtpSent(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not send code");
    } finally {
      setBusy(false);
    }
  }

  async function handleVerifyPhone() {
    setError("");
    setBusy(true);
    try {
      await verifyPhoneOtp(user?.id, phone, otp);
      setPhoneDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Invalid code");
    } finally {
      setBusy(false);
    }
  }

  async function handleContinueIdentity() {
    setError("");
    const message = validateIdentifyNumber(identityNumber);
    setIdentityError(message);
    if (message) return;

    setBusy(true);
    try {
      await verifyIdentityNumber(user?.id, identityNumber);
      setIdentityDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Invalid identity number");
    } finally {
      setBusy(false);
    }
  }

  async function handleStartScan() {
    setScanning(true);
    setTimeout(async () => {
      setScanning(false);
      setVerifying(true);
      try {
        const profile = await verifyFace(user?.id);
        setFinalProfile(profile);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Face verification failed");
      } finally {
        setVerifying(false);
      }
    }, 1600);
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>Identity Verification</h1>

        <div className={styles.stepper}>
          {STEPS.map((label, idx) => (
            <div key={label} className={styles.stepperItem}>
              <span
                className={
                  idx < stepIndex || finalProfile
                    ? `${styles.dot} ${styles.dotDone}`
                    : idx === stepIndex
                      ? `${styles.dot} ${styles.dotActive}`
                      : styles.dot
                }
              >
                {idx < stepIndex || finalProfile ? "✓" : idx + 1}
              </span>
              <span className={styles.stepLabel}>{label}</span>
              {idx < STEPS.length - 1 && <span className={styles.stepLine} />}
            </div>
          ))}
        </div>

        {error && <p className={styles.errorBox}>{error}</p>}

        {finalProfile ? (
          <div className={styles.result}>
            <div className={styles.resultCheck}>✓</div>
            <h2 className={styles.resultTitle}>Identity Verified</h2>
            <p className={styles.resultSubtitle}>Your identity has been successfully verified.</p>
            <div className={styles.resultRows}>
              <div className={styles.resultRow}>
                <span>Full Name</span>
                <strong>{finalProfile.fullName}</strong>
              </div>
              <div className={styles.resultRow}>
                <span>Email</span>
                <strong>{finalProfile.email} <em className={styles.matchTag}>✓ Matched</em></strong>
              </div>
              <div className={styles.resultRow}>
                <span>Phone</span>
                <strong>{finalProfile.phone} <em className={styles.matchTag}>✓ Matched</em></strong>
              </div>
              <div className={styles.resultRow}>
                <span>Identity Number</span>
                <strong>••••{finalProfile.identityNumber.slice(-4)} <em className={styles.matchTag}>✓ Verified</em></strong>
              </div>
              <div className={styles.resultRow}>
                <span>Face Verification</span>
                <strong><em className={styles.matchTag}>✓ Matched</em></strong>
              </div>
              <div className={styles.resultRow}>
                <span>Status</span>
                <strong className={styles.statusVerified}>VERIFIED</strong>
              </div>
            </div>
            <button className={styles.primaryBtn} onClick={() => navigate(returnTo)}>
              Continue
            </button>
          </div>
        ) : stepIndex === 0 ? (
          <div className={styles.stepBody}>
            <h3 className={styles.stepHeading}>Verify Phone Number</h3>
            {!phoneDone ? (
              <>
                <label className={styles.label}>Phone Number</label>
                <input
                  className={styles.input}
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="0901234567"
                />
                {!otpSent ? (
                  <button className={styles.primaryBtn} disabled={busy || !phone} onClick={handleSendOtp}>
                    {busy ? "Sending..." : "Send OTP"}
                  </button>
                ) : (
                  <>
                    <label className={styles.label}>Verification Code</label>
                    <input
                      className={styles.input}
                      value={otp}
                      onChange={(e) => setOtp(e.target.value.replace(/\D/g, "").slice(0, 6))}
                      placeholder="6-digit code"
                      maxLength={6}
                    />
                    <p className={styles.hint}>Didn't receive the code? Use 123456 for this demo.</p>
                    <button
                      className={styles.primaryBtn}
                      disabled={busy || otp.length < 6}
                      onClick={handleVerifyPhone}
                    >
                      {busy ? "Verifying..." : "Verify Phone"}
                    </button>
                  </>
                )}
              </>
            ) : (
              <div className={styles.miniResult}>
                <p className={styles.miniResultText}>✓ Phone Number Verified — {phone}</p>
                <button className={styles.primaryBtn} onClick={() => setStepIndex(1)}>
                  Continue →
                </button>
              </div>
            )}
          </div>
        ) : stepIndex === 1 ? (
          <div className={styles.stepBody}>
            <h3 className={styles.stepHeading}>Identity Information</h3>
            {!identityDone ? (
              <>
                <p className={styles.hint}>
                  Enter your identity number registered with our trusted identity provider.
                </p>
                <label className={styles.label}>Identity Number</label>
                <input
                  className={styles.input}
                  value={identityNumber}
                  onChange={(e) => setIdentityNumber(e.target.value.replace(/\D/g, "").slice(0, 12))}
                  placeholder="079xxxxxxxxx"
                />
                {identityError && <p className={styles.errorBox}>{identityError}</p>}
                <div className={styles.stepActions}>
                  <button className={styles.secondaryBtn} onClick={() => setStepIndex(0)}>
                    ← Back
                  </button>
                  <button
                    className={styles.primaryBtn}
                    disabled={busy || !identityNumber}
                    onClick={handleContinueIdentity}
                  >
                    {busy ? "Checking..." : "Continue →"}
                  </button>
                </div>
              </>
            ) : (
              <div className={styles.miniResult}>
                <p className={styles.miniResultText}>✓ Identity number confirmed</p>
                <button className={styles.primaryBtn} onClick={() => setStepIndex(2)}>
                  Continue →
                </button>
              </div>
            )}
          </div>
        ) : (
          <div className={styles.stepBody}>
            <h3 className={styles.stepHeading}>Face Verification</h3>
            {!scanning && !verifying && (
              <>
                <div className={styles.cameraBox}>
                  <div className={styles.cameraDot} />
                  <p>CAMERA</p>
                </div>
                <p className={styles.hint}>Position your face inside the frame. Make sure your face is clear.</p>
                <button className={styles.primaryBtn} onClick={handleStartScan}>
                  Start Scan
                </button>
              </>
            )}
            {scanning && (
              <div className={styles.scanBox}>
                <div className={styles.cameraDot} />
                <p className={styles.scanText}>Face Detected</p>
                <ul className={styles.checklist}>
                  <li>✓ Look straight</li>
                  <li>✓ Turn your head left</li>
                  <li>✓ Blink</li>
                </ul>
              </div>
            )}
            {verifying && (
              <div className={styles.scanBox}>
                <p className={styles.scanText}>Verifying Identity...</p>
                <ul className={styles.checklist}>
                  <li>Phone verification ✓</li>
                  <li>Identity number ✓</li>
                  <li>Face matching …</li>
                  <li>Email matching …</li>
                  <li>Phone matching …</li>
                </ul>
                <p className={styles.hint}>Checking with trusted identity provider...</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
