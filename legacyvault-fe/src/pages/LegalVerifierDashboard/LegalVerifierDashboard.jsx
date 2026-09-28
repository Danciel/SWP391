import { useCallback, useEffect, useState } from "react";
import { getLegalVerifierDashboard, reviewLegalCase, flagSuspectedFraud } from "../../api/verifierApi";
import styles from "./LegalVerifierDashboard.module.css";

const STATUS_LABEL = {
  AWAITING_DOCUMENTS: "Awaiting Documents",
  PENDING_REVIEW: "Pending Review",
  APPROVED: "Approved",
  REJECTED: "Rejected",
};

function Badge({ status }) {
  return (
    <span className={`${styles.badge} ${styles[status] || ""}`}>
      {STATUS_LABEL[status] || status}
    </span>
  );
}

export default function LegalVerifierDashboard() {
  const [data, setData] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);
  const [toast, setToast] = useState("");
  const [rejectingId, setRejectingId] = useState(null);
  const [reason, setReason] = useState("");
  const [expandedId, setExpandedId] = useState(null);
  const [flaggingId, setFlaggingId] = useState(null);
  const [flagReason, setFlagReason] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const result = await getLegalVerifierDashboard();
      setData(result);
    } catch (err) {
      setError(err.message || "Could not load legal case data");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function showToast(msg) {
    setToast(msg);
    setTimeout(() => setToast(""), 2500);
  }

  async function handleApprove(caseId) {
    setBusyId(caseId);
    try {
      await reviewLegalCase({ caseId, approve: true });
      showToast("Case approved");
      await load();
    } catch (err) {
      setError(err.message || "Could not approve case");
    } finally {
      setBusyId(null);
    }
  }

  async function handleReject(caseId) {
    setBusyId(caseId);
    try {
      await reviewLegalCase({ caseId, approve: false, reason });
      showToast("Case rejected");
      setRejectingId(null);
      setReason("");
      await load();
    } catch (err) {
      setError(err.message || "Could not reject case");
    } finally {
      setBusyId(null);
    }
  }

  async function handleFlagFraud(caseId) {
    setBusyId(caseId);
    try {
      await flagSuspectedFraud({ caseId, reason: flagReason });
      showToast("Case flagged as suspected fraud — sent to Admin");
      setFlaggingId(null);
      setFlagReason("");
      await load();
    } catch (err) {
      setError(err.message || "Could not flag case");
    } finally {
      setBusyId(null);
    }
  }

  const pendingCount = data?.cases.filter((c) => c.status === "PENDING_REVIEW").length || 0;

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div>
          <h1 className={styles.title}>Legal Verifier Dashboard</h1>
          <p className={styles.subtitle}>
            Receive legal cases from Executors, review submitted records, and approve or reject.
          </p>
        </div>
        {data && <div className={styles.statPill}>{pendingCount} awaiting review</div>}
      </header>

      {toast && <div className={styles.toast}>{toast}</div>}
      {isLoading && <p className={styles.info}>Loading data...</p>}
      {error && <p className={styles.errorBox}>{error}</p>}

      {data && !isLoading && (
        <section className={styles.card}>
          <h2 className={styles.cardTitle}>Legal Cases ({data.cases.length})</h2>
          {data.cases.length === 0 ? (
            <p className={styles.mutedText}>No legal cases yet</p>
          ) : (
            <ul className={styles.list}>
              {data.cases.map((c) => {
                const overdue = c.status === "PENDING_REVIEW" && c.deadlineAt && new Date(c.deadlineAt) < new Date();
                return (
                <li key={c.id} className={styles.listItem}>
                  <div className={styles.itemMain}>
                    <strong>{c.ownerName}</strong>
                    <div className={styles.metaText}>
                      Executor: {c.executorName} ·{" "}
                      {c.submittedAt
                        ? `Submitted ${formatDate(c.submittedAt)}`
                        : "Not yet submitted"}
                    </div>
                    {c.status === "PENDING_REVIEW" && (
                      <div className={overdue ? styles.deadlineOverdue : styles.metaText}>
                        {overdue ? "Overdue — " : "Respond by "}
                        {formatDate(c.deadlineAt)}
                      </div>
                    )}
                    {c.flagged && (
                      <div className={styles.flagText}>⚠ Flagged as suspected fraud{c.flagReason ? `: ${c.flagReason}` : ""}</div>
                    )}
                    {c.status === "REJECTED" && c.rejectionReason && (
                      <div className={styles.reasonText}>Reason: {c.rejectionReason}</div>
                    )}
                    <button
                      className={styles.linkBtn}
                      onClick={() => setExpandedId(expandedId === c.id ? null : c.id)}
                    >
                      {expandedId === c.id ? "Hide details" : "View details"}
                    </button>
                    {expandedId === c.id && (
                      <div className={styles.detailBox}>
                        <div className={styles.detailRow}>
                          <span className={styles.detailLabel}>Owner (deceased)</span>
                          <span>{c.ownerName}</span>
                        </div>
                        <div className={styles.detailRow}>
                          <span className={styles.detailLabel}>Declared Executor</span>
                          <span>{c.executorDeclarant || c.executorName}</span>
                        </div>
                        <div className={styles.detailRow}>
                          <span className={styles.detailLabel}>Death Certificate</span>
                          <span>
                            {c.deathCertificate
                              ? `${c.deathCertificate.fileName} (uploaded ${formatDate(c.deathCertificate.uploadedAt)})`
                              : "Not submitted yet"}
                          </span>
                        </div>
                      </div>
                    )}
                  </div>

                  <div className={styles.itemRight}>
                    <Badge status={c.status} />
                    {c.status === "PENDING_REVIEW" && rejectingId !== c.id && flaggingId !== c.id && (
                      <div className={styles.actionRow}>
                        <button
                          className={styles.primaryBtn}
                          disabled={busyId === c.id}
                          onClick={() => handleApprove(c.id)}
                        >
                          Approve
                        </button>
                        <button
                          className={styles.dangerBtn}
                          disabled={busyId === c.id}
                          onClick={() => setRejectingId(c.id)}
                        >
                          Reject
                        </button>
                        {!c.flagged && (
                          <button
                            className={styles.secondaryBtn}
                            disabled={busyId === c.id}
                            onClick={() => setFlaggingId(c.id)}
                          >
                            Flag Suspected Fraud
                          </button>
                        )}
                      </div>
                    )}
                    {rejectingId === c.id && (
                      <div className={styles.rejectForm}>
                        <input
                          className={styles.reasonInput}
                          placeholder="Reason for rejection"
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                        />
                        <div className={styles.actionRow}>
                          <button
                            className={styles.dangerBtn}
                            disabled={busyId === c.id}
                            onClick={() => handleReject(c.id)}
                          >
                            Confirm Reject
                          </button>
                          <button
                            className={styles.secondaryBtn}
                            onClick={() => {
                              setRejectingId(null);
                              setReason("");
                            }}
                          >
                            Cancel
                          </button>
                        </div>
                      </div>
                    )}
                    {flaggingId === c.id && (
                      <div className={styles.rejectForm}>
                        <input
                          className={styles.reasonInput}
                          placeholder="Why is this suspicious?"
                          value={flagReason}
                          onChange={(e) => setFlagReason(e.target.value)}
                        />
                        <div className={styles.actionRow}>
                          <button
                            className={styles.dangerBtn}
                            disabled={busyId === c.id}
                            onClick={() => handleFlagFraud(c.id)}
                          >
                            Confirm Flag
                          </button>
                          <button
                            className={styles.secondaryBtn}
                            onClick={() => {
                              setFlaggingId(null);
                              setFlagReason("");
                            }}
                          >
                            Cancel
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                </li>
                );
              })}
            </ul>
          )}
        </section>
      )}
    </div>
  );
}

function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("en-US");
}
