import { useState, useEffect, useRef } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getProfile } from "../../api/vaultApi";
import { sendPhoneOtp, verifyPhoneOtp } from "../../api/phoneApi";
import { linkIdp } from "../../api/ownerApi";
import { searchIdpUsers } from "../../api/idpUserApi"; // Added import
import { ApiError } from "../../api/client";
import styles from "./VerifyIdentity.module.css";

const STEPS = ["Phone", "Identity", "Face"];

export default function VerifyIdentity() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnTo = params.get("returnTo") || "/dashboard/profile";

  // --- General State ---
  const [stepIndex, setStepIndex] = useState(0);
  const [finalProfile, setFinalProfile] = useState(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  // --- Step 1: Phone State ---
  const [otp, setOtp] = useState("");
  const [otpSent, setOtpSent] = useState(false);
  const [phoneDone, setPhoneDone] = useState(false);
  const [phone, setPhone] = useState(user?.phone || "");
  const [loadingProfile, setLoadingProfile] = useState(true);

  // --- Step 2: Identity State (Now stores UUID) ---
  const [idpUserId, setIdpUserId] = useState("");
  const [identityDone, setIdentityDone] = useState(false);
  const [identityError, setIdentityError] = useState("");
  const [searchingIdp, setSearchingIdp] = useState(false);

  // --- Step 3: Face State ---
  const videoRef = useRef(null);
  const streamRef = useRef(null);
  const [preview, setPreview] = useState("");
  const [photoFile, setPhotoFile] = useState(null);
  const [cameraError, setCameraError] = useState("");
  const [verifying, setVerifying] = useState(false);

  // Fetch full profile on mount
  useEffect(() => {
    let isMounted = true;
    async function loadProfile() {
      try {
        const profile = await getProfile();
        if (!isMounted) return;

        if (profile.phone) setPhone(profile.phone);
        if (profile.isPhoneVerified || profile.phoneVerified) {
          setPhoneDone(true);
          setStepIndex(1);
        }
      } catch (err) {
        if (isMounted) setError("Could not load profile data.");
      } finally {
        if (isMounted) setLoadingProfile(false);
      }
    }

    if (!user?.phone) loadProfile();
    else setLoadingProfile(false);

    return () => { isMounted = false; };
  }, [user]);

  // Handle Camera Lifecycle for Step 3
  useEffect(() => {
    let active = true;

    async function startCamera() {
      if (stepIndex !== 2 || preview || finalProfile) return;

      try {
        if (!navigator.mediaDevices?.getUserMedia) throw new Error("Browser does not support camera.");
        const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" }, audio: false });

        if (!active) {
          stream.getTracks().forEach((track) => track.stop());
          return;
        }

        streamRef.current = stream;
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          await videoRef.current.play();
        }
      } catch (err) {
        if (active) setCameraError(err.name === "NotAllowedError" ? "Camera permission denied." : err.message);
      }
    }

    startCamera();

    return () => {
      active = false;
      if (streamRef.current) streamRef.current.getTracks().forEach((track) => track.stop());
    };
  }, [stepIndex, preview, finalProfile]);

  // --- Step 1 Handlers ---
  async function handleSendOtp() {
    setError(""); setBusy(true);
    try {
      await sendPhoneOtp();
      setOtpSent(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not send code");
    } finally { setBusy(false); }
  }

  async function handleVerifyPhone() {
    setError(""); setBusy(true);
    try {
      await verifyPhoneOtp(otp);
      setPhoneDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Invalid code");
    } finally { setBusy(false); }
  }

  // --- Step 2 Handlers ---
  async function handleAutoDetectIdp() {
    setIdentityError(""); setSearchingIdp(true);
    try {
      // Search the IDP database using the logged-in user's email or phone
      const res = await searchIdpUsers({ email: user?.email, phone: user?.phone });

      // Depending on your API response structure, it might return an array or a single object
      const idpUser = Array.isArray(res) ? res[0] : res;

      if (idpUser && idpUser.id) {
        setIdpUserId(idpUser.id);
      } else {
        setIdentityError("Could not auto-detect your Identity Provider ID. Please enter it manually.");
      }
    } catch (err) {
      setIdentityError("Lookup failed. Please enter the UUID manually.");
    } finally {
      setSearchingIdp(false);
    }
  }

  async function handleContinueIdentity() {
    setError("");
    if (!idpUserId.trim()) {
      setIdentityError("IDP User ID (UUID) is required.");
      return;
    }
    setIdentityDone(true);
  }

  // --- Step 3 Handlers ---
  function capturePhoto() {
    const video = videoRef.current;
    if (!video || !video.videoWidth || !video.videoHeight) {
      setCameraError("Camera not ready. Please try again.");
      return;
    }

    const canvas = document.createElement("canvas");
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const context = canvas.getContext("2d");
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    canvas.toBlob((blob) => {
      if (!blob) return setCameraError("Failed to capture image.");
      setPreview(URL.createObjectURL(blob));
      setPhotoFile(new File([blob], "face-verify.jpg", { type: "image/jpeg" }));
      setCameraError("");
    }, "image/jpeg", 0.9);
  }

  function retakePhoto() {
    if (preview) URL.revokeObjectURL(preview);
    setPreview(""); setPhotoFile(null); setCameraError("");
  }

  async function handleVerifyFace() {
    if (!photoFile) {
      setCameraError("Please capture a photo first.");
      return;
    }

    if (!idpUserId) {
      setError("Missing IDP User ID. Please go back.");
      return;
    }

    setVerifying(true);
    setError("");

    try {
      await linkIdp(idpUserId, photoFile);

      const profile = await getProfile();
      setFinalProfile(profile);
    } catch (err) {
      console.error("Face verification error:", err);
      // Backend now says WHY (IdP unreachable, no enrolled face, mismatch, ...).
      const detail = (err?.message || "").trim();
      setError(
        detail
          ? `Face verification failed: ${detail}`
          : "Face verification failed. Please make sure only your face is in the frame and try again."
      );
    } finally {
      setVerifying(false);
    }
  }

  if (loadingProfile) {
    return <div className={styles.page}><div className={styles.card}><p className={styles.hint}>Loading profile...</p></div></div>;
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>Identity Verification</h1>

        <div className={styles.stepper}>
          {STEPS.map((label, idx) => (
            <div key={label} className={styles.stepperItem}>
              <span className={idx < stepIndex || finalProfile ? `${styles.dot} ${styles.dotDone}` : idx === stepIndex ? `${styles.dot} ${styles.dotActive}` : styles.dot}>
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
              <div className={styles.resultRow}><span>Full Name</span><strong>{finalProfile.fullName}</strong></div>
              <div className={styles.resultRow}><span>Email</span><strong>{finalProfile.email} <em className={styles.matchTag}>✓ Matched</em></strong></div>
              <div className={styles.resultRow}><span>Phone</span><strong>{finalProfile.phone} <em className={styles.matchTag}>✓ Matched</em></strong></div>
              <div className={styles.resultRow}><span>Face Verification</span><strong><em className={styles.matchTag}>✓ Matched</em></strong></div>
              <div className={styles.resultRow}><span>Status</span><strong className={styles.statusVerified}>VERIFIED</strong></div>
            </div>
            <button className={styles.primaryBtn} onClick={() => navigate(returnTo)}>Continue</button>
          </div>
        ) : stepIndex === 0 ? (
          <div className={styles.stepBody}>
            <h3 className={styles.stepHeading}>Verify Phone Number</h3>
            {!phoneDone ? (
              <>
                <label className={styles.label}>Phone Number</label>
                <input className={styles.input} value={phone} readOnly placeholder="Not registered" />
                {!otpSent ? (
                  <button className={styles.primaryBtn} disabled={busy || !phone} onClick={handleSendOtp}>
                    {busy ? "Sending..." : "Send OTP"}
                  </button>
                ) : (
                  <>
                    <label className={styles.label}>Verification Code</label>
                    <input className={styles.input} value={otp} onChange={(e) => setOtp(e.target.value.replace(/\D/g, "").slice(0, 6))} placeholder="6-digit code" maxLength={6} />
                    <button className={styles.primaryBtn} disabled={busy || otp.length < 6} onClick={handleVerifyPhone}>
                      {busy ? "Verifying..." : "Verify Phone"}
                    </button>
                  </>
                )}
              </>
            ) : (
              <div className={styles.miniResult}>
                <p className={styles.miniResultText}>✓ Phone Number Verified — {phone}</p>
                <button className={styles.primaryBtn} onClick={() => setStepIndex(1)}>Continue →</button>
              </div>
            )}
          </div>
        ) : stepIndex === 1 ? (
          <div className={styles.stepBody}>
            <h3 className={styles.stepHeading}>Identity Provider Link</h3>
            {!identityDone ? (
              <>
                <p className={styles.hint}>
                  Enter your IDP User ID (UUID). You can copy this from the database, or use the auto-detect button.
                </p>

                <label className={styles.label}>IDP User ID (UUID)</label>
                <input
                  className={styles.input}
                  value={idpUserId}
                  onChange={(e) => setIdpUserId(e.target.value)} // Removed numeric restriction
                  placeholder="e.g. 530a1905-d658-4424-a08a-85ca5c80..."
                />

                {identityError && <p className={styles.errorBox}>{identityError}</p>}

                <div className={styles.stepActions} style={{ marginBottom: "1rem" }}>
                  <button
                    className={styles.secondaryBtn}
                    onClick={handleAutoDetectIdp}
                    disabled={searchingIdp}
                  >
                    {searchingIdp ? "Searching..." : "🔍 Auto-detect my ID"}
                  </button>
                </div>

                <div className={styles.stepActions}>
                  <button className={styles.secondaryBtn} onClick={() => setStepIndex(0)}>← Back</button>
                  <button className={styles.primaryBtn} disabled={busy || !idpUserId.trim()} onClick={handleContinueIdentity}>
                    Continue →
                  </button>
                </div>
              </>
            ) : (
              <div className={styles.miniResult}>
                <p className={styles.miniResultText}>✓ IDP User ID confirmed</p>
                <button className={styles.primaryBtn} onClick={() => setStepIndex(2)}>Continue →</button>
              </div>
            )}
          </div>
        ) : (
          <div className={styles.stepBody}>
            <h3 className={styles.stepHeading}>Face Verification</h3>
            {cameraError && <p className={styles.errorBox}>{cameraError}</p>}

            {!preview ? (
              <>
                <div className={styles.cameraBox} style={{ position: "relative", overflow: "hidden" }}>
                  <video ref={videoRef} autoPlay muted playsInline style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                </div>
                <p className={styles.hint}>Position your face inside the frame.</p>
                <button className={styles.primaryBtn} onClick={capturePhoto}>Capture Photo</button>
              </>
            ) : (
              <>
                <div className={styles.cameraBox} style={{ position: "relative", overflow: "hidden" }}>
                  <img src={preview} alt="Captured face" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                </div>
                {verifying ? (
                  <div className={styles.scanBox}>
                    <p className={styles.scanText}>Verifying Identity...</p>
                  </div>
                ) : (
                  <div className={styles.stepActions}>
                    <button className={styles.secondaryBtn} onClick={retakePhoto}>Retake</button>
                    <button className={styles.primaryBtn} onClick={handleVerifyFace}>Verify Face</button>
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}