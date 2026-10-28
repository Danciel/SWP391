import { useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  getInheritedWill,
  claimInheritedItem,
  revealInheritedSecret,
} from "../../api/inheritanceApi";
import SecurityPinModal from "../../components/SecurityPinModal/SecurityPinModal";
import FaceCaptureModal from "../../components/FaceCaptureModal/FaceCaptureModal";
import styles from "./InheritedWillDetails.module.css";

const TYPE_LABEL = { IMMEDIATE: "Immediate handover", DEATH: "Inheritance after death" };

export default function InheritedWillDetails() {
  const { id } = useParams();
  const [grant, setGrant] = useState(null);
  const [will, setWill] = useState(null);
  const [pinOpen, setPinOpen] = useState(true);
  const [faceTargets, setFaceTargets] = useState(null); // item ids to receive
  const [secret, setSecret] = useState(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const pendingRef = useRef(null); // action to run once a fresh PIN grant exists

  async function loadWill(g) {
    setWill(await getInheritedWill(id, g));
  }

  // Runs after the PIN is accepted (the modal shows any error thrown here).
  async function handlePinVerified(g) {
    setGrant(g);
    const action = pendingRef.current;
    pendingRef.current = null;
    if (action) await action(g);
    else await loadWill(g);
    setPinOpen(false);
  }

  // The PIN grant lives a few minutes: when it ran out, ask for the PIN again.
  function askPinThen(action) {
    pendingRef.current = action;
    setPinOpen(true);
  }

  async function refreshAfterClaim() {
    try {
      await loadWill(grant);
    } catch (err) {
      if (err?.status === 423) askPinThen(loadWill);
      else setError(err.message || "Could not refresh the will.");
    }
  }

  // One face scan receives every selected asset.
  async function handleFaceCaptured(blob) {
    const ids = faceTargets || [];
    for (const itemId of ids) {
      await claimInheritedItem(itemId, blob);
    }
    setFaceTargets(null);
    setNotice(
      ids.length === 1
        ? "Asset received. You can now view its credentials."
        : `${ids.length} assets received. You can now view their credentials.`
    );
    await refreshAfterClaim();
  }

  async function handleReveal(item) {
    setError("");
    const run = async (g) => {
      const r = await revealInheritedSecret(id, item.itemId, g);
      setSecret({ name: item.assetName, data: r.secretData || {} });
    };

    try {
      await run(grant);
    } catch (err) {
      if (err?.status === 423 || !grant) askPinThen(run);
      else setError(err.message || "Could not open the credentials.");
    }
  }

  const items = will?.items || [];
  const ready = items.filter((i) => i.canClaim);
  const allClaimed = items.length > 0 && items.every((i) => i.claimedAt);

  return (
    <div className={styles.page}>
      <Link to="/dashboard/view-will" className={styles.backLink}>
        ← Back to Inherited Wills
      </Link>

      {!will && (
        <div className={styles.card}>
          <h1 className={styles.title}>Inherited will</h1>
          <p className={styles.hintSmall}>
            Enter your 6-digit Security PIN to open this will.
          </p>
          {error && <p className={styles.error}>{error}</p>}
          {!pinOpen && (
            <button className={styles.primaryBtn} onClick={() => setPinOpen(true)}>
              Enter PIN
            </button>
          )}
        </div>
      )}

      {will && (
        <div className={styles.card}>
          <div className={styles.headRow}>
            <h1 className={styles.title}>{will.willTitle}</h1>
            {allClaimed && <span className={styles.claimedTag}>RECEIVED ✓</span>}
          </div>
          <p className={styles.metaLine}>From: {will.ownerName}</p>
          {will.description && <p className={styles.metaLine}>{will.description}</p>}

          <div className={styles.statusGrid}>
            <StatusRow label="Handover type" value={TYPE_LABEL[will.handoverType] || will.handoverType} ok />
            <StatusRow label="Handover status" value={will.handoverStatus.replaceAll("_", " ")} ok={will.handoverStatus !== "PAUSED"} />
            <StatusRow
              label="Your identity"
              value={will.faceLinked ? "FACE ENROLLED" : "NOT VERIFIED"}
              ok={will.faceLinked}
            />
          </div>
          <hr className={styles.divider} />

          <h2 className={styles.sectionTitle}>Your assets</h2>
          <div className={styles.assetList}>
            {items.map((a) => (
              <div key={a.itemId} className={styles.assetRow}>
                <div>
                  <p className={styles.itemTitle}>{a.assetName}</p>
                  <p className={styles.itemMeta}>
                    {a.assetType}
                    {a.provider ? ` · ${a.provider}` : ""} · {a.allocationPercentage}%
                  </p>
                </div>

                {a.claimedAt ? (
                  <button className={styles.secondaryBtn} onClick={() => handleReveal(a)}>
                    View credentials
                  </button>
                ) : a.canClaim ? (
                  <span className={styles.accessibleTag}>Ready to receive</span>
                ) : (
                  <span className={styles.lockedTag}>⏳ Waiting for the Executor</span>
                )}
              </div>
            ))}
          </div>

          {notice && <p className={styles.okText}>{notice}</p>}
          {error && <p className={styles.error}>{error}</p>}

          {!will.faceLinked && ready.length > 0 && (
            <p className={styles.hintSmall}>
              Verify your identity first so your face can be matched.{" "}
              <Link to={`/dashboard/verify-identity?returnTo=${encodeURIComponent(`/dashboard/view-will/${id}`)}`}>
                Verify identity
              </Link>
            </p>
          )}

          {ready.length > 0 && (
            <>
              <p className={styles.hintSmall}>
                Your Executor has handed {ready.length === 1 ? "this asset" : "these assets"} over.
                Scan your face to receive {ready.length === 1 ? "it" : "them"} into your account.
              </p>
              <button
                className={styles.primaryBtn}
                disabled={!will.faceLinked || will.handoverStatus === "PAUSED"}
                onClick={() => {
                  setError("");
                  setNotice("");
                  setFaceTargets(ready.map((i) => i.itemId));
                }}
              >
                Scan face to receive {ready.length === 1 ? "asset" : `${ready.length} assets`}
              </button>
            </>
          )}
        </div>
      )}

      {pinOpen && (
        <SecurityPinModal
          scope="INHERITANCE_VIEW"
          resourceId={id}
          onVerified={handlePinVerified}
          onClose={() => {
            pendingRef.current = null;
            setPinOpen(false);
          }}
        />
      )}

      {faceTargets && (
        <FaceCaptureModal
          title="Scan your face to receive"
          hint="Look straight at the camera in good light. Your face is matched with the one you enrolled."
          onCapture={handleFaceCaptured}
          onClose={() => setFaceTargets(null)}
        />
      )}

      {secret && (
        <div className={styles.secretOverlay} onClick={() => setSecret(null)}>
          <div className={styles.secretModal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.sectionTitle}>{secret.name}</h2>
            {Object.keys(secret.data).length === 0 && (
              <p className={styles.itemMeta}>No credentials stored for this asset.</p>
            )}
            {Object.entries(secret.data).map(([k, v]) => (
              <div key={k} className={styles.secretRow}>
                <span className={styles.itemMeta}>{k}</span>
                <code className={styles.secretValue}>{String(v)}</code>
              </div>
            ))}
            <div className={styles.actions}>
              <button className={styles.primaryBtn} onClick={() => setSecret(null)}>Close</button>
            </div>
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
