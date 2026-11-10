import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getWills, requestImmediateHandover, deleteWillDraft } from "../../api/vaultApi";
import { dmsMessage, dmsState } from "../../utils/willHelpers";
import { useBackdropClose } from "../../utils/useBackdropClose";
import SecurityPinModal from "../../components/SecurityPinModal/SecurityPinModal";
import styles from "./MyWills.module.css";

const FILTERS = ["All", "Draft", "Pending", "Active"];

const STATUS_LABEL = {
  DRAFT: "Draft",
  PENDING_EXECUTOR_ACCEPTANCE: "Pending Executor",
  ACTIVE: "Active",
  PENDING_EXECUTOR: "Pending Executor",
  NEEDS_EXECUTOR: "Needs Executor",
  PENDING_VERIFICATION: "Pending Verification",
  DECLINED: "Declined",
  HANDOVER_IN_PROGRESS: "Handover in progress",
  HANDED_OVER: "Handed over",
};

function matchesFilter(status, filter) {
  if (filter === "All") return true;
  if (filter === "Draft") return status === "DRAFT" || status === "DECLINED";
  if (filter === "Pending") return status === "PENDING_EXECUTOR" || status === "PENDING_EXECUTOR_ACCEPTANCE" || status === "NEEDS_EXECUTOR";
  if (filter === "Active") return status === "ACTIVE" || status === "PENDING_VERIFICATION" || status === "HANDOVER_IN_PROGRESS" || status === "HANDED_OVER";
  return true;
}

export default function MyWills() {
  const { user } = useAuth();
  const [wills, setWills] = useState([]);
  const [filter, setFilter] = useState("All");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [handoverWill, setHandoverWill] = useState(null);
  const [handoverBusy, setHandoverBusy] = useState(false);
  const [handoverPinWill, setHandoverPinWill] = useState(null);
  const [handoverError, setHandoverError] = useState("");
  const [deleteWill, setDeleteWill] = useState(null);
  const [deleteBusy, setDeleteBusy] = useState(false);
  const handoverBackdrop = useBackdropClose(() => {
    if (!handoverBusy) setHandoverWill(null);
  });
  const deleteBackdrop = useBackdropClose(() => {
    if (!deleteBusy) setDeleteWill(null);
  });
  const [deleteError, setDeleteError] = useState("");

  useEffect(() => {
    load();
  }, [user]);

  async function load() {
    setLoading(true);
    setLoadError("");
    try {
      setWills(await getWills(user?.id));
    } catch (err) {
      setWills([]);
      setLoadError(err.message || "Could not load wills");
    } finally {
      setLoading(false);
    }
  }

  // "Yes" only opens the PIN dialog; the handover starts with the PIN grant.
  function handleConfirmImmediateHandover() {
    setHandoverError("");
    setHandoverPinWill(handoverWill);
    setHandoverWill(null);
  }

  async function handleHandoverPinVerified(grant) {
    await requestImmediateHandover(user?.id, handoverPinWill.id, grant);
    setHandoverPinWill(null);
    load();
  }

  async function handleConfirmDelete() {
    setDeleteBusy(true);
    setDeleteError("");
    try {
      await deleteWillDraft(user?.id, deleteWill.id, deleteWill.isDeclined);
      setDeleteWill(null);
      load();
    } catch (err) {
      setDeleteError(err.message || "Could not delete draft");
    } finally {
      setDeleteBusy(false);
    }
  }

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

      {loadError && <p role="alert" className={styles.muted}>{loadError}</p>}
      {loading ? (
        <p className={styles.muted}>Loading...</p>
      ) : filtered.length === 0 ? (
        <p className={styles.muted}>No wills in this category yet.</p>
      ) : (
        <div className={styles.list}>
          {filtered.map((will) => {
            const isDraft = will.status === "DRAFT";
            const isDeclined = will.executor?.status === "DECLINED" || will.status === "DECLINED";
            return (
              <div key={will.id} className={styles.willCard}>
                <div className={styles.willHead}>
                  <h3 className={styles.willName}>{will.name || "Untitled Will"}</h3>
                  <span className={`${styles.badge} ${styles[will.status] || ""}`}>
                    {isDeclined ? "Declined" : STATUS_LABEL[will.status] || will.status}
                  </span>
                </div>
                <div className={styles.metaGrid}>
                  <span>Assets: {will.assets ? will.assets.length : "—"}</span>
                  <span>Beneficiaries: {will.beneficiaries ? will.beneficiaries.length : "—"}</span>
                  {will.executor && <span>Executor: {will.executor.name}</span>}
                  {will.payment?.status && will.payment.status !== "NONE" && (
                    <span>Payment: {will.payment.status}</span>
                  )}
                  {will.dms?.status && <span>DMS: {will.dms.status}</span>}
                </div>
                {(() => {
                  const note = will.dms ? dmsMessage(dmsState(will.dms)) : null;
                  return note ? (
                    <p role="alert" className={styles.errorText} style={note.tone === "warn" ? { color: "#92400e" } : undefined}>
                      {note.text}{" "}
                      <Link to={`/dashboard/my-wills/${will.id}`}>Open to check in</Link>
                    </p>
                  ) : null;
                })()}
                <div className={styles.cardActions}>
                  <Link
                    className={styles.viewBtn}
                    to={isDraft ? `/dashboard/create-will?willId=${will.id}` : `/dashboard/my-wills/${will.id}`}
                  >
                    {isDraft ? "Continue Editing" : "View Details"}
                  </Link>
                  {will.status === "ACTIVE" && (
                    <button
                      className={styles.handoverBtn}
                      onClick={() => {
                        setHandoverError("");
                        setHandoverWill(will);
                      }}
                    >
                      Immediate Handover
                    </button>
                  )}
                  {(isDraft || isDeclined) && (
                    <button
                      className={styles.deleteBtn}
                      onClick={() => {
                        setDeleteError("");
                        setDeleteWill({...will, isDeclined});
                      }}
                    >
                      Delete
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {handoverWill && (
        <div className={styles.overlay} {...handoverBackdrop}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Are you sure?</h2>
            <p className={styles.modalMeta}>
              This will send "{handoverWill.name}" to {handoverWill.executor?.name || "your executor"} to
              hand its assets over now, while you're still alive. Your executor will confirm the
              beneficiary details before anything is delivered.
            </p>
            {handoverError && <p className={styles.errorText}>{handoverError}</p>}
            <div className={styles.modalActions}>
              <button
                className={styles.secondaryBtn}
                disabled={handoverBusy}
                onClick={() => setHandoverWill(null)}
              >
                Cancel
              </button>
              <button
                className={styles.primaryBtn}
                disabled={handoverBusy}
                onClick={handleConfirmImmediateHandover}
              >
                {handoverBusy ? "Sending..." : "Yes, Enter PIN"}
              </button>
            </div>
          </div>
        </div>
      )}

      {handoverPinWill && (
        <SecurityPinModal
          scope="HANDOVER_START"
          resourceId={handoverPinWill.id}
          onVerified={handleHandoverPinVerified}
          onClose={() => setHandoverPinWill(null)}
        />
      )}

      {deleteWill && (
        <div className={styles.overlay} {...deleteBackdrop}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Delete this Will?</h2>
            <p className={styles.modalMeta}>
              This will permanently delete "{deleteWill.name || "Untitled Will"}". This action cannot be
              undone.
            </p>
            {deleteError && <p className={styles.errorText}>{deleteError}</p>}
            <div className={styles.modalActions}>
              <button
                className={styles.secondaryBtn}
                disabled={deleteBusy}
                onClick={() => setDeleteWill(null)}
              >
                Cancel
              </button>
              <button
                className={styles.dangerBtn}
                disabled={deleteBusy}
                onClick={handleConfirmDelete}
              >
                {deleteBusy ? "Deleting..." : "Yes, Delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
