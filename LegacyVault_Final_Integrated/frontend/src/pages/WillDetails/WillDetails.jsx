import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getWill, checkInDms } from "../../api/willsApi";
import { ApiError } from "../../api/client";
import styles from "./WillDetails.module.css";

function beneficiaryForAsset(will, willAssetId) {
  const assignment = (will.assignments || []).find((a) => a.willAssetId === willAssetId);
  return (will.beneficiaries || []).find((b) => b.id === assignment?.beneficiaryDesignationId) || null;
}

export default function WillDetails() {
  const { id } = useParams();
  const [will, setWill] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [checkingIn, setCheckingIn] = useState(false);

  useEffect(() => {
    load();
  }, [id]);

  async function load() {
    setLoading(true);
    setError("");
    try {
      setWill(await getWill(id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load will");
    } finally {
      setLoading(false);
    }
  }

  async function handleCheckIn() {
    setCheckingIn(true);
    setError("");
    try {
      await checkInDms(id);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not check in");
    } finally {
      setCheckingIn(false);
    }
  }

  if (loading) return <p className={styles.muted}>Loading...</p>;
  if (error && !will) return <p className={styles.errorText}>{error}</p>;
  if (!will) return <p className={styles.muted}>Will not found.</p>;

  return (
    <div className={styles.page}>
      <Link className={styles.backLink} to="/dashboard/my-wills">← My Wills</Link>
      <div className={styles.headRow}>
        <h1 className={styles.title}>{will.title}</h1>
        <span className={`${styles.badge} ${styles[will.status] || ""}`}>{will.status}</span>
      </div>
      {error && <p className={styles.errorText}>{error}</p>}

      <div className={styles.card}>
        <Row label="Description" value={will.description || "—"} />
        <Row label="Created" value={new Date(will.createdAt).toLocaleString()} />
        <Row label="Submitted" value={will.submittedAt ? new Date(will.submittedAt).toLocaleString() : "—"} />
        {will.status === "DRAFT" && (
          <div className={styles.actions}>
            <Link className={styles.primaryBtn} to={`/dashboard/create-will?willId=${will.id}`}>Continue Editing</Link>
          </div>
        )}
      </div>

      <div className={styles.card}>
        <h2>Assets & Beneficiaries</h2>
        {(will.assets || []).map((asset) => {
          const b = beneficiaryForAsset(will, asset.willAssetId);
          return (
            <div key={asset.willAssetId} className={styles.assetRow}>
              <div><p className={styles.itemTitle}>{asset.assetName}</p><p className={styles.itemMeta}>{asset.assetType}</p></div>
              <div><p className={styles.itemMeta}>Beneficiary</p><p className={styles.itemTitle}>{b?.fullName || "—"}</p><p className={styles.itemMeta}>{b?.relationship || ""}</p></div>
            </div>
          );
        })}
      </div>

      <div className={styles.card}>
        <h2>Executor</h2>
        <Row label="Name" value={will.executor?.executorName || "—"} />
        <Row label="Status" value={will.executor?.status || "—"} />
      </div>

      <div className={styles.card}>
        <h2>Dead Man's Switch</h2>
        {will.dms ? (
          <>
            <Row label="Status" value={will.dms.status} />
            <Row label="Interval" value={`${will.dms.checkInIntervalDays} days`} />
            <Row label="Missed" value={`${will.dms.missedCheckIns}/${will.dms.maxMissedCheckIns}`} />
            <Row label="Last Check-in" value={will.dms.lastCheckInAt ? new Date(will.dms.lastCheckInAt).toLocaleString() : "—"} />
            <Row label="Next Check-in" value={will.dms.nextCheckInAt ? new Date(will.dms.nextCheckInAt).toLocaleString() : "—"} />
            {will.dms.status === "ACTIVE" && (
              <div className={styles.actions}>
                <button className={styles.primaryBtn} disabled={checkingIn} onClick={handleCheckIn}>{checkingIn ? "Checking in..." : "I'm Still Active / Check In"}</button>
              </div>
            )}
          </>
        ) : <p className={styles.hintSmall}>DMS not configured.</p>}
      </div>
    </div>
  );
}

function Row({ label, value }) {
  return <div className={styles.row}><span className={styles.rowLabel}>{label}</span><span className={styles.rowValue}>{value}</span></div>;
}
