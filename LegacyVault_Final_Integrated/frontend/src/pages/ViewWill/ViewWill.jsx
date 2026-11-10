import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getInheritedWills } from "../../api/vaultApi";
import styles from "./ViewWill.module.css";

const CLAIM_LABEL = {
  WAITING_FOR_EXECUTOR: "WAITING FOR EXECUTOR",
  READY_TO_CLAIM: "READY TO RECEIVE",
  CLAIMED: "RECEIVED",
};

export default function ViewWill() {
  const [wills, setWills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    getInheritedWills()
      .then((rows) => setWills(Array.isArray(rows) ? rows : []))
      .catch((err) => setError(err.message || "Could not load inherited wills"))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Inherited Wills</h1>
      <p className={styles.subtitle}>
        Wills that name you as a beneficiary. Open one with your Security PIN, then scan
        your face to receive the assets.
      </p>

      {error && <p role="alert" className={styles.muted}>{error}</p>}
      {loading ? (
        <p className={styles.muted}>Loading...</p>
      ) : wills.length === 0 ? (
        <p className={styles.muted}>No inheritance has been handed over to you yet.</p>
      ) : (
        <div className={styles.list}>
          {wills.map((w) => (
            <div key={`${w.willId}-${w.startedAt}`} className={styles.card}>
              <div className={styles.cardHead}>
                <h3 className={styles.willName}>{w.willTitle}</h3>
                {w.claimStatus === "CLAIMED" && <span className={styles.claimedTag}>RECEIVED ✓</span>}
              </div>
              <p className={styles.metaLine}>From: {w.ownerName}</p>
              <p className={styles.metaLine}>
                Your assets: {w.totalItems} · ready: {w.readyItems} · received: {w.claimedItems}
              </p>
              <p className={styles.metaLine}>
                Status:{" "}
                <span className={w.claimStatus === "READY_TO_CLAIM" ? styles.readyTag : styles.mutedTag}>
                  {CLAIM_LABEL[w.claimStatus] || w.claimStatus}
                </span>
              </p>
              <Link className={styles.viewBtn} to={`/dashboard/view-will/${w.willId}`}>
                Open with PIN
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
