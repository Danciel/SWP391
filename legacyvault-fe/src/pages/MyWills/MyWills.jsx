import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getWills } from "../../api/mockVault";
import styles from "./MyWills.module.css";

const FILTERS = ["All", "Draft", "Pending", "Active"];

const STATUS_LABEL = {
  DRAFT: "Draft",
  PENDING_EXECUTOR_ACCEPTANCE: "Pending Executor",
  ACTIVE: "Active",
  NEEDS_EXECUTOR: "Needs Executor",
  PENDING_VERIFICATION: "Pending Verification",
};

function matchesFilter(status, filter) {
  if (filter === "All") return true;
  if (filter === "Draft") return status === "DRAFT";
  if (filter === "Pending") return status === "PENDING_EXECUTOR_ACCEPTANCE" || status === "NEEDS_EXECUTOR";
  if (filter === "Active") return status === "ACTIVE" || status === "PENDING_VERIFICATION";
  return true;
}

export default function MyWills() {
  const { user } = useAuth();
  const [wills, setWills] = useState([]);
  const [filter, setFilter] = useState("All");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getWills(user?.id).then((data) => {
      setWills(data);
      setLoading(false);
    });
  }, [user]);

  const filtered = wills.filter((w) => matchesFilter(w.status, filter));

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>My Wills</h1>
      <p className={styles.subtitle}>Manage the wills you have created.</p>

      <div className={styles.filterRow}>
        {FILTERS.map((f) => (
          <button
            key={f}
            className={filter === f ? styles.filterActive : styles.filter}
            onClick={() => setFilter(f)}
          >
            {f}
          </button>
        ))}
      </div>

      {loading ? (
        <p className={styles.muted}>Loading...</p>
      ) : filtered.length === 0 ? (
        <p className={styles.muted}>No wills in this category yet.</p>
      ) : (
        <div className={styles.list}>
          {filtered.map((will) => {
            const isDraft = will.status === "DRAFT";
            return (
              <div key={will.id} className={styles.willCard}>
                <div className={styles.willHead}>
                  <h3 className={styles.willName}>{will.name || "Untitled Will"}</h3>
                  <span className={`${styles.badge} ${styles[will.status] || ""}`}>
                    {STATUS_LABEL[will.status] || will.status}
                  </span>
                </div>
                <div className={styles.metaGrid}>
                  <span>Assets: {(will.assets || []).length}</span>
                  <span>Beneficiaries: {(will.assets || []).filter((a) => a.beneficiary).length}</span>
                  {will.executor && <span>Executor: {will.executor.name}</span>}
                  {will.payment?.status && will.payment.status !== "NONE" && (
                    <span>Payment: {will.payment.status}</span>
                  )}
                  {will.dms?.status && <span>DMS: {will.dms.status}</span>}
                </div>
                <Link
                  className={styles.viewBtn}
                  to={isDraft ? `/dashboard/create-will?willId=${will.id}` : `/dashboard/my-wills/${will.id}`}
                >
                  {isDraft ? "Continue Editing" : "View Details"}
                </Link>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
