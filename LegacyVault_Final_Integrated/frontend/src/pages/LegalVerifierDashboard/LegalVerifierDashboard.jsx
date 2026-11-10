import { useCallback, useEffect, useState } from "react";

import {
  getLegalVerifierDashboard,
  getLegalCaseDetails,
  getLegalCaseCertificate,
  reviewLegalCase,
} from "../../api/verifierApi";
import { fmtCertDate } from "../../utils/deathCertificate";

import styles from "./LegalVerifierDashboard.module.css";

const STATUS_LABEL = {
  AWAITING_DOCUMENTS: "Awaiting Documents",
  PENDING: "Pending Review",
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
  const [checks, setChecks] = useState({});
  const [certPreview, setCertPreview] = useState({});
  const [caseDetails, setCaseDetails] = useState({});

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


  async function handleViewCertificate(caseId) {
    let url;

    try {
      const response = await getLegalCaseCertificate(caseId);
      const blob = await response.blob();

      if (!blob.size) {
        throw new Error("The certificate file is empty.");
      }

      url = URL.createObjectURL(blob);

      const openedWindow = window.open(
        url,
        "_blank",
        "noopener,noreferrer"
      );

      if (!openedWindow) {
        const link = document.createElement("a");
        link.href = url;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        link.click();
      }
    } catch (err) {
      setError(err.message || "Could not open death certificate.");
    }
  }


  // Loads the uploaded certificate so the verifier reads it right here.
  async function loadCertificatePreview(caseId) {
    try {
      const response = await getLegalCaseCertificate(caseId);
      const blob = await response.blob();
      if (!blob.size) throw new Error("The certificate file is empty.");
      setCertPreview((prev) => ({
        ...prev,
        [caseId]: { url: URL.createObjectURL(blob), type: blob.type },
      }));
    } catch (err) {
      setCertPreview((prev) => ({ ...prev, [caseId]: { error: err.message } }));
    }
  }

  function setCheck(caseId, key, value) {
    setChecks((prev) => ({ ...prev, [caseId]: { ...prev[caseId], [key]: value } }));
  }

  async function handleViewCaseDetails(caseId) {
    if (expandedId === caseId) {
      setExpandedId(null);
      return;
    }

    setExpandedId(caseId);

    if (caseDetails[caseId]) return;
    loadCertificatePreview(caseId);

    try {
      const detail = await getLegalCaseDetails(caseId);

      if (!detail || typeof detail !== "object") {
        throw new Error("Invalid Legal Case details response.");
      }

      setCaseDetails((prev) => ({
        ...prev,
        [caseId]: detail,
      }));
    } catch (err) {
      setError(err.message || "Could not load case details.");
    }
  }

  const pendingCount =
    data?.cases.filter((c) =>
      ["PENDING", "PENDING_REVIEW"].includes(c.status)
    ).length || 0;

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
                const overdue = ["PENDING", "PENDING_REVIEW"].includes(c.status) && c.deadlineAt && new Date(c.deadlineAt) < new Date();
                const detail = caseDetails[c.id] || c;
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
                      {["PENDING", "PENDING_REVIEW"].includes(c.status) && (
                        <div className={overdue ? styles.deadlineOverdue : styles.metaText}>
                          {overdue ? "Overdue — " : "Respond by "}
                          {formatDate(c.deadlineAt)}
                        </div>
                      )}
                      {c.status === "REJECTED" && (c.reviewNote || c.rejectionReason) && (
                        <div className={styles.reasonText}>Reason: {c.reviewNote || c.rejectionReason}</div>
                      )}
                      <button
                        className={styles.linkBtn}
                        onClick={() => handleViewCaseDetails(c.id)}
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
                              {detail.originalFileName || "Not submitted yet"}
                              {detail.originalFileName && (
                                <button
                                  type="button"
                                  className={styles.linkBtn}
                                  onClick={() => handleViewCertificate(c.id)}
                                >
                                  View certificate
                                </button>
                              )}
                            </span>
                          </div>


                          {(() => {
                            const cert = parseCert(detail.certificateData);
                            const preview = certPreview[c.id];
                            const nameMatch = cert
                              ? sameName(cert.deceasedName, detail.ownerName)
                              : null;
                            const thresholdOk =
                              detail.maxMissedCheckIns > 0 &&
                              detail.missedCheckIns >= detail.maxMissedCheckIns;
                            const pending = ["PENDING", "PENDING_REVIEW"].includes(c.status);
                            const mine = checks[c.id] || {};

                            return (
                              <div className={styles.willSection}>
                                <div className={styles.willTitle}>1. Read the death certificate</div>
                                {!preview && <p className={styles.mutedText}>Loading certificate...</p>}
                                {preview?.error && <p className={styles.errorBox}>{preview.error}</p>}
                                {preview?.url && (
                                  preview.type === "application/pdf" ? (
                                    <iframe
                                      title="Death certificate"
                                      src={preview.url}
                                      style={{ width: "100%", height: 520, border: "1px solid #d1d5db", borderRadius: 8 }}
                                    />
                                  ) : (
                                    <img
                                      alt="Death certificate"
                                      src={preview.url}
                                      style={{ width: "100%", maxWidth: 560, border: "1px solid #d1d5db", borderRadius: 8 }}
                                    />
                                  )
                                )}

                                <div className={styles.willTitle} style={{ marginTop: 14 }}>
                                  2. Cross-check with the system
                                </div>
                                <CheckRow
                                  ok={thresholdOk}
                                  label="Dead Man's Switch threshold reached"
                                  value={`${detail.missedCheckIns ?? 0} / ${detail.maxMissedCheckIns ?? 0} missed check-ins`}
                                />
                                <CheckRow
                                  ok={detail.lifeStatus === "DECEASED"}
                                  neutral={detail.lifeStatus === "UNKNOWN"}
                                  label="Life status from the identity provider"
                                  value={detail.lifeStatus || "UNKNOWN"}
                                />
                                {cert ? (
                                  <>
                                    <CheckRow
                                      ok={nameMatch}
                                      label="Name on certificate vs. Owner account"
                                      value={`${cert.deceasedName || "—"}  vs  ${detail.ownerName || "—"}`}
                                    />
                                    <div className={styles.detailRow}>
                                      <span className={styles.detailLabel}>Date / place of death</span>
                                      <span>{fmtCertDate(cert.dateOfDeath)} · {cert.placeOfDeath || "—"}</span>
                                    </div>
                                    <div className={styles.detailRow}>
                                      <span className={styles.detailLabel}>Cause of death</span>
                                      <span>{cert.causeOfDeath || "—"}</span>
                                    </div>
                                    <div className={styles.detailRow}>
                                      <span className={styles.detailLabel}>Registered by</span>
                                      <span>
                                        {cert.issuingAuthority || "—"} · No. {cert.registrationNumber || "—"} ·{" "}
                                        {cert.registrarName || "—"}
                                      </span>
                                    </div>
                                  </>
                                ) : (
                                  <p className={styles.mutedText}>
                                    The Executor uploaded a scanned file without form data: compare the name on
                                    the file with the Owner ({detail.ownerName || "—"}) yourself.
                                  </p>
                                )}
                                <div className={styles.detailRow}>
                                  <span className={styles.detailLabel}>Will</span>
                                  <span>
                                    {detail.willTitle || "—"} · {detail.assetCount ?? 0} asset(s) ·{" "}
                                    {detail.beneficiaryCount ?? 0} beneficiary(ies)
                                  </span>
                                </div>

                                {pending && (
                                  <>
                                    <div className={styles.willTitle} style={{ marginTop: 14 }}>
                                      3. Confirm
                                    </div>
                                    <label className={styles.verifyCheck}>
                                      <input
                                        type="checkbox"
                                        checked={!!mine.read}
                                        disabled={!preview?.url}
                                        onChange={(e) => setCheck(c.id, "read", e.target.checked)}
                                      />
                                      I opened and read the death certificate.
                                    </label>
                                    <label className={styles.verifyCheck}>
                                      <input
                                        type="checkbox"
                                        checked={!!mine.match}
                                        onChange={(e) => setCheck(c.id, "match", e.target.checked)}
                                      />
                                      The certificate matches the Owner and the Will, and has a valid seal and signature.
                                    </label>
                                  </>
                                )}
                              </div>
                            );
                          })()}
                        </div>
                      )}
                    </div>

                    <div className={styles.itemRight}>
                      <Badge status={c.status} />
                      {["PENDING", "PENDING_REVIEW"].includes(c.status) && rejectingId !== c.id && (
                        <div className={styles.actionRow}>

                          <button
                            className={styles.primaryBtn}
                            disabled={busyId === c.id || !(checks[c.id]?.read && checks[c.id]?.match)}
                            title={
                              !caseDetails[c.id]?.originalFileName
                                ? "Open details and load the death certificate first"
                                : !(checks[c.id]?.read && checks[c.id]?.match)
                                  ? "Tick both confirmation boxes first"
                                  : ""
                            }
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
                          {!(checks[c.id]?.read && checks[c.id]?.match) && (
                            <div className={styles.metaText}>Open “View details”, read the certificate and confirm to enable Approve.</div>
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

function parseCert(raw) {
  if (!raw) return null;
  try {
    const v = JSON.parse(raw);
    return v && typeof v === "object" ? v : null;
  } catch {
    return null;
  }
}

function norm(s) {
  return String(s || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .replace(/\s+/g, " ")
    .trim();
}

function sameName(a, b) {
  return norm(a) !== "" && norm(a) === norm(b);
}

function CheckRow({ ok, neutral, label, value }) {
  const mark = neutral ? "?" : ok ? "✓" : "✗";
  const color = neutral ? "#92400e" : ok ? "#166534" : "#b91c1c";
  return (
    <div className={styles.detailRow}>
      <span className={styles.detailLabel}>{label}</span>
      <span style={{ color, fontWeight: 600 }}>{mark} {value}</span>
    </div>
  );
}

function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("en-US");
}
