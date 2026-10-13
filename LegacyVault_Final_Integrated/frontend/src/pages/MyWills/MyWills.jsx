import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getWills, getWill, deleteWill } from "../../api/willsApi";
import { ApiError } from "../../api/client";
import styles from "./MyWills.module.css";

const FILTERS = ["All", "Draft", "Pending", "Active"];
const STATUS_LABEL = {
  DRAFT: "Draft",
  PENDING_EXECUTOR: "Pending Executor",
  ACTIVE: "Active",
};

function matchesFilter(status, filter) {
  if (filter === "All") return true;
  if (filter === "Draft") return status === "DRAFT";
  if (filter === "Pending") return status === "PENDING_EXECUTOR";
  if (filter === "Active") return status === "ACTIVE";
  return true;
}

export default function MyWills() {
  const [wills, setWills] = useState([]);
  const [filter, setFilter] = useState("All");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    setLoading(true);
    setError("");
    try {
      const summaries = await getWills();
      const details = await Promise.all((summaries || []).map((w) => getWill(w.id)));
      setWills(details);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load wills");
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    setDeleteBusy(true);
    try {
      await deleteWill(deleteTarget.id);
      setDeleteTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not delete draft");
    } finally {
      setDeleteBusy(false);
    }
  }

  const filtered = wills.filter((w) => matchesFilter(w.status, filter));

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>My Wills</h1>
      <p className={styles.subtitle}>Wills loaded from the real backend.</p>
      {error && <p className={styles.errorText}>{error}</p>}

      <div className={styles.filterRow}>
        {FILTERS.map((f) => (
          <button key={f} className={filter === f ? styles.filterActive : styles.filter} onClick={() => setFilter(f)}>{f}</button>
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
                  <h3 className={styles.willName}>{will.title || "Untitled Will"}</h3>
                  <span className={`${styles.badge} ${styles[will.status] || ""}`}>{STATUS_LABEL[will.status] || will.status}</span>
                </div>
                <div className={styles.metaGrid}>
                  <span>Assets: {will.assets?.length || 0}</span>
                  <span>Beneficiaries: {will.beneficiaries?.length || 0}</span>
                  {will.executor && <span>Executor: {will.executor.executorName}</span>}
                  {will.dms && <span>DMS: {will.dms.status}</span>}
                </div>
                <div className={styles.cardActions}>
                  <Link className={styles.viewBtn} to={isDraft ? `/dashboard/create-will?willId=${will.id}` : `/dashboard/my-wills/${will.id}`}>
                    {isDraft ? "Continue Editing" : "View Details"}
                  </Link>
                  {isDraft && <button className={styles.deleteBtn} onClick={() => setDeleteTarget(will)}>Delete</button>}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {deleteTarget && (
        <div className={styles.overlay} onClick={() => !deleteBusy && setDeleteTarget(null)}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Delete this draft?</h2>
            <p className={styles.modalMeta}>This will permanently delete "{deleteTarget.title}".</p>
            <div className={styles.modalActions}>
              <button className={styles.secondaryBtn} disabled={deleteBusy} onClick={() => setDeleteTarget(null)}>Cancel</button>
              <button className={styles.dangerBtn} disabled={deleteBusy} onClick={handleDelete}>{deleteBusy ? "Deleting..." : "Yes, Delete"}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
