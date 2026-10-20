import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import {
  getInheritedWill,
  getProfile,
  verifyFace,
  issueClaimKey,
  confirmClaim,
} from "../../api/vaultApi";
import { ApiError } from "../../api/client";
import styles from "./InheritedWillDetails.module.css";

export default function InheritedWillDetails() {
  const { id } = useParams();
  const { user } = useAuth();
  const [will, setWill] = useState(null);
  const [profile, setProfile] = useState(null);
  const [stage, setStage] = useState("view");
  const [claimKey, setClaimKey] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    load();
  }, [id]);

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [w, p] = await Promise.all([getInheritedWill(user?.id, id), getProfile(user?.id, user)]);
      setWill(w);
      setProfile(p);
      setStage("view");
    } catch (err) {
      setError(err.message || "Inherited will details not available");
    } finally {
      setLoading(false);
    }
  }

  async function handleClaimClick() {
    setError("");
    if (profile.identityStatus !== "VERIFIED") {
      setStage("identityGate");
      return;
    }
    setStage("faceScan");
  }

  function handleStartScan() {
    setStage("faceVerifying");
    setBusy(true);
    setTimeout(async () => {
      try {
        const updatedProfile = await verifyFace(user?.id);
        setProfile(updatedProfile);
        const key = await issueClaimKey(user?.id, id);
        setClaimKey(key);
        setStage("claimKey");
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Face verification failed");
        setStage("faceScan");
      } finally {
        setBusy(false);
      }
    }, 1600);
  }

  async function handleConfirmClaim() {
    setError("");
    setBusy(true);
    try {
      const updated = await confirmClaim(user?.id, id, claimKey.code);
      setWill(updated);
      setStage("view");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not confirm claim");
    } finally {
      setBusy(false);
    }
  }

  if (loading) return <p className={styles.muted}>Loading...</p>;
  if (error && (!will || !profile)) return <p role="alert" className={styles.muted}>{error}</p>;
  if (!will || !profile) return <p className={styles.muted}>No inheritance information available.</p>;

  const claimed = will.claimStatus === "CLAIMED";

  return (
    <div className={styles.page}>
      <Link to="/dashboard/view-will" className={styles.backLink}>
        ← Back to Inherited Wills
      </Link>

      {stage === "view" && (
        <div className={styles.card}>
          <div className={styles.headRow}>
            <h1 className={styles.title}>{will.willName}</h1>
            {claimed && <span className={styles.claimedTag}>CLAIMED ✓</span>}
          </div>
          <p className={styles.metaLine}>From: {will.from}</p>

          {!claimed && (
            <>
              <div className={styles.statusGrid}>
                <StatusRow label="Owner Life Status" value="DECEASED" ok />
                <StatusRow label="Legal Verification" value="APPROVED" ok />
                <StatusRow label="Your Identity" value={profile.identityStatus.replaceAll("_", " ")} ok={profile.identityStatus === "VERIFIED"} />
                <StatusRow label="Claim Status" value={will.claimStatus.replaceAll("_", " ")} ok />
              </div>
              <hr className={styles.divider} />
            </>
          )}

          <h2 className={styles.sectionTitle}>{claimed ? "Your Inherited Assets" : "Your Assigned Assets"}</h2>
          <div className={styles.assetList}>
            {will.assets.map((a) => (
              <div key={a.id} className={styles.assetRow}>
                <div>
                  <p className={styles.itemTitle}>{a.name}</p>
                  <p className={styles.itemMeta}>Type: {a.type}</p>
                </div>
                {claimed ? (
                  <span className={styles.accessibleTag}>✓ Accessible</span>
                ) : (
                  <span className={styles.lockedTag}>🔒 Locked</span>
                )}
              </div>
            ))}
          </div>

          {!claimed && (
            <>
              <p className={styles.hintSmall}>
                Claim this will to access the asset content (actual documents, credentials, and decrypted files stay
                hidden until then).
              </p>
              {error && <p className={styles.error}>{error}</p>}
              <button className={styles.primaryBtn} onClick={handleClaimClick}>
                Claim Will
              </button>
            </>
          )}
        </div>
      )}

      {stage === "identityGate" && (
        <div className={styles.card}>
          <h2 className={styles.title}>Identity Verification Required</h2>
          <p className={styles.hintSmall}>You must verify your identity before claiming this will.</p>
          <p className={styles.itemMeta}>Identity Status: ⚠ NOT VERIFIED</p>
          <p className={styles.itemMeta}>Verification includes: Phone verification · Identity number · Face verification</p>
          <div className={styles.actions}>
            <a
              className={styles.primaryBtn}
              href={`/dashboard/verify-identity?returnTo=${encodeURIComponent(`/dashboard/view-will/${id}`)}`}
            >
              Verify Identity
            </a>
          </div>
        </div>
      )}

      {stage === "faceScan" && (
        <div className={styles.card}>
          <h2 className={styles.title}>Claim Will</h2>
          <p className={styles.metaLine}>{will.willName} — From: {will.from}</p>
          <p className={styles.itemMeta}>Identity Status: ✓ VERIFIED</p>
          <p className={styles.hintSmall}>Please verify your face before receiving your Claim Key.</p>
          <button className={styles.primaryBtn} onClick={handleStartScan}>
            Verify Face
          </button>
        </div>
      )}

      {stage === "faceVerifying" && (
        <div className={styles.card}>
          <h2 className={styles.title}>Face Verification</h2>
          <div className={styles.scanBox}>
            <div className={styles.cameraDot} />
            <p className={styles.scanText}>{busy ? "Matching face..." : "Face Detected"}</p>
          </div>
        </div>
      )}

      {stage === "claimKey" && claimKey && (
        <div className={styles.card}>
          <h2 className={styles.title}>Claim Key Issued</h2>
          <p className={styles.metaLine}>{will.willName}</p>
          <p className={styles.itemMeta}>Face Verification: ✓ VERIFIED</p>
          <div className={styles.claimKeyBox}>{claimKey.code}</div>
          <p className={styles.itemMeta}>Valid Until: {new Date(claimKey.expiresAt).toLocaleString()}</p>
          <p className={styles.hintSmall}>This Claim Key is valid only for {will.willName}.</p>
          <button className={styles.primaryBtn} onClick={() => setStage("confirm")}>
            Continue to Claim
          </button>
        </div>
      )}

      {stage === "confirm" && claimKey && (
        <div className={styles.card}>
          <h2 className={styles.title}>Claim Will</h2>
          <p className={styles.metaLine}>{will.willName} — From: {will.from}</p>
          <p className={styles.itemMeta}>Identity: ✓ VERIFIED</p>
          <p className={styles.itemMeta}>Face Verification: ✓ VERIFIED</p>
          <p className={styles.itemMeta}>Claim Key: {claimKey.code}</p>
          {error && <p className={styles.error}>{error}</p>}
          <div className={styles.actions}>
            <button className={styles.secondaryBtn} onClick={() => setStage("view")}>
              Cancel
            </button>
            <button className={styles.primaryBtn} disabled={busy} onClick={handleConfirmClaim}>
              {busy ? "Processing..." : "Confirm & Claim Will"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function StatusRow({ label, value, ok }) {
  return (
    <div className={styles.statusRow}>
      <span className={styles.itemMeta}>{label}</span>
      <span className={ok ? styles.okText : styles.warnText}>{ok ? "✓" : "⚠"} {value}</span>
    </div>
  );
}
