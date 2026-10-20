import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getInheritedWills } from "../../api/vaultApi";
import styles from "./ViewWill.module.css";

export default function ViewWill() {
  const { user } = useAuth();
  const [wills, setWills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    getInheritedWills(user?.id)
      .then(setWills)
      .catch((err) => setError(err.message || "Could not load inherited wills"))
      .finally(() => setLoading(false));
  }, [user]);

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Inherited Wills</h1>
      <p className={styles.subtitle}>View inheritance cases assigned to you.</p>

      {error && <p role="alert" className={styles.muted}>{error}</p>}
      {loading ? (
        <p className={styles.muted}>Loading...</p>
      ) : wills.length === 0 ? (
        <p className={styles.muted}>No inheritance cases assigned to you yet.</p>
      ) : (
        <div className={styles.list}>
          {wills.map((w) => (
            <div key={w.id} className={styles.card}>
              <div className={styles.cardHead}>
                <h3 className={styles.willName}>{w.willName}</h3>
                {w.claimStatus === "CLAIMED" && <span className={styles.claimedTag}>CLAIMED ✓</span>}
              </div>
              <p className={styles.metaLine}>From: {w.from}</p>
              <p className={styles.metaLine}>Assigned Assets: {w.assets.length}</p>
              <p className={styles.metaLine}>
                Claim Status:{" "}
                <span className={w.claimStatus === "READY_TO_CLAIM" ? styles.readyTag : styles.mutedTag}>
                  {w.claimStatus.replaceAll("_", " ")}
                </span>
              </p>
              <Link className={styles.viewBtn} to={`/dashboard/view-will/${w.id}`}>
                View Details
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
