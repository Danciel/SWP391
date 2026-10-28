import { useCallback, useEffect, useState } from "react";
import {
  getExecutorDashboard,
  respondToExecutorRequest,
  openExecutorCase,
  submitExecutorLegalDocuments,
  verifyCaseProfile,
  respondToHandoverRequest,
  deleteExecutorRequest,
} from "../../api/executorApi";
import { getExecutorProfile ,updateMyExecutorPricing } from "../../api/vaultApi";
import ExecutorPricingCard, { EvaluationCsvActions } from "../../components/ExecutorPricingCard/ExecutorPricingCard";
import useExecutorPricing from "../../components/ExecutorPricingCard/useExecutorPricing";
import SecurityPinModal from "../../components/SecurityPinModal/SecurityPinModal";
import { getStoredUser } from "../../api/client";
import styles from "./ExecutorDashboard.module.css";

const STATUS_LABEL = {
  PENDING: "Pending",
  ACCEPTED: "Accepted",
  DECLINED: "Declined",
  ACTIVE: "Active",
  AWAITING_CASE: "Awaiting Case",
  CASE_OPENED: "Case Opened",
  THRESHOLD_REACHED: "Threshold Reached",
  TRIGGERED: "Triggered",
  COMPLETED: "Completed",
  CLOSED: "Closed",
  OPEN: "Open",
  SUBMITTED_FOR_REVIEW: "Submitted for Review",
  APPROVED: "Approved",
  REJECTED: "Rejected",
  CONFIRMED: "Confirmed",
};

function isOverdue(deadlineAt) {
  return Boolean(deadlineAt) && new Date(deadlineAt) < new Date();
}

function formatDeadline(deadlineAt) {
  if (!deadlineAt) return null;
  const overdue = isOverdue(deadlineAt);
  return {
    overdue,
    text: `${overdue ? "Overdue — " : "Respond by "}${formatDate(deadlineAt)}`,
  };
}

function Badge({ status }) {
  return (
    <span className={`${styles.badge} ${styles[status] || ""}`}>
      {STATUS_LABEL[status] || status}
    </span>
  );
}

export default function ExecutorDashboard() {
  const [data, setData] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);
  const [toast, setToast] = useState("");
  const [expandedId, setExpandedId] = useState(null);
  const [fee, setFee] = useState(null);
  const [feeInput, setFeeInput] = useState("");
  const [editingFee, setEditingFee] = useState(false);
  const [savingFee, setSavingFee] = useState(false);
  const [uploadCaseId, setUploadCaseId] = useState(null);
  const [uploadFiles, setUploadFiles] = useState([]);
  const [uploadWillId, setUploadWillId] = useState(null);
  const [uploadFile, setUploadFile] = useState(null);
  // Delete a declined request: 1) confirm popup  2) PIN popup
  const [deleteConfirmReq, setDeleteConfirmReq] = useState(null);
  const [deletePinReq, setDeletePinReq] = useState(null);

  const pricing = useExecutorPricing({
    onFeeUpdated: (updated) => {
      const next = {
        ...fee,
        fee: updated.serviceFee,
        currency: updated.currency || fee?.currency || "VND",
        updatedAt: updated.updatedAt || new Date().toISOString(),
      };
      setFee(next);
      setFeeInput(String(next.fee));
    },
  });

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const result = await getExecutorDashboard();
      setData(result);

      const user = getStoredUser();

      if (user?.id) {
        const profile = await getExecutorProfile(user.id);

        const currentFee = {
          fee: profile.serviceFee ?? null,
          currency: profile.currency || "VND",
          updatedAt: profile.updatedAt || null,
        };

        setFee(currentFee);
        setFeeInput(
          currentFee.fee != null ? String(currentFee.fee) : ""
        );
      } else {
        setFee({ fee: null, currency: "VND", updatedAt: null });
        setFeeInput("");
      }
    } catch (err) {
      setError(err.message || "Could not load executor data");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function toggleExpanded(id) {
    setExpandedId((prev) => (prev === id ? null : id));
  }


  async function handleSaveFee() {
    const amount = Number(feeInput);

    if (feeInput.trim() === "" || !Number.isFinite(amount) || amount < 0) {
      setError("Service fee must be a valid non-negative number.");
      return;
    }

    setSavingFee(true);
    setError("");

    try {
      const updated = await updateMyExecutorPricing({
        serviceFee: amount,
        currency: fee?.currency || "VND",
      });

      const updatedFee = {
        ...fee,
        fee: updated.serviceFee ?? amount,
        currency: updated.currency || fee?.currency || "VND",
        updatedAt: updated.updatedAt || new Date().toISOString(),
      };

      setFee(updatedFee);
      setFeeInput(String(updatedFee.fee));
      setEditingFee(false);
      showToast("Service fee updated");
    } catch (err) {
      setError(err.message || "Could not update fee");
    } finally {
      setSavingFee(false);
    }
  }

  async function handleVerifyProfile(caseId) {
    setBusyId(caseId);
    try {
      await verifyCaseProfile({ caseId });
      showToast("Owner profile verified");
      await load();
    } catch (err) {
      setError(err.message || "Could not verify profile");
    } finally {
      setBusyId(null);
    }
  }

  function showToast(msg) {
    setToast(msg);
    setTimeout(() => setToast(""), 2500);
  }

  async function handleRequest(requestId, accept) {
    setBusyId(requestId);
    try {
      await respondToExecutorRequest({ requestId, accept });
      showToast(accept ? "Request accepted" : "Request declined");
      await load();
    } catch (err) {
      setError(err.message || "Action failed");
    } finally {
      setBusyId(null);
    }
  }

  function startDeleteRequest(req) {
    setError("");
    setDeleteConfirmReq(req);
  }

  function continueDeleteToPin() {
    setDeletePinReq(deleteConfirmReq);
    setDeleteConfirmReq(null);
  }

  async function handleDeleteVerified(grant) {
    const req = deletePinReq;
    if (!req) return;
    await deleteExecutorRequest({ requestId: req.id, pinGrant: grant });
    setDeletePinReq(null);
    if (expandedId === req.id) setExpandedId(null);
    showToast("Declined request deleted");
    await load();
  }

  async function handleOpenCase(triggerId) {
    setBusyId(triggerId);
    try {
      await openExecutorCase({ triggerId });
      showToast("Case opened");
      await load();
    } catch (err) {
      setError(err.message || "Could not open case");
    } finally {
      setBusyId(null);
    }
  }

  function openUploadPopup(caseId) {
    setUploadCaseId(caseId);
    setUploadFiles([]);
  }

  function closeUploadPopup() {
    setUploadCaseId(null);
    setUploadFiles([]);
  }

  function handleFilesChosen(e) {
    setUploadFiles(Array.from(e.target.files || []).map((f) => f.name));
  }

  async function handleConfirmUpload() {
    const caseId = uploadCaseId;
    setBusyId(caseId);
    try {
      await submitExecutorLegalDocuments({
        caseId,
        documentNames: uploadFiles.length > 0 ? uploadFiles : ["death_certificate.pdf", "id_verification.pdf"],
      });
      showToast("Legal documents submitted");
      closeUploadPopup();
      await load();
    } catch (err) {
      setError(err.message || "Could not submit documents");
    } finally {
      setBusyId(null);
    }
  }

  async function handleHandoverResponse(requestId, confirm) {
    setBusyId(requestId);
    try {
      await respondToHandoverRequest({ requestId, confirm });
      showToast(confirm ? "Handover confirmed" : "Handover declined");
      await load();
    } catch (err) {
      setError(err.message || "Action failed");
    } finally {
      setBusyId(null);
    }
  }


  function openLegalDocumentUpload(willId) {
    setUploadWillId(willId);
    setUploadFile(null);
    setError("");
  }

  function closeLegalDocumentUpload() {
    setUploadWillId(null);
    setUploadFile(null);
  }

  function handleLegalDocumentSelected(event) {
    const file = event.target.files?.[0] ?? null;
    setUploadFile(file);
  }

  async function handleLegalDocumentSubmit() {
    if (!uploadWillId) {
      setError("Missing Will ID.");
      return;
    }

    if (!uploadFile) {
      setError("Please select a document.");
      return;
    }

    setBusyId(uploadWillId);
    setError("");

    try {
      await submitExecutorLegalDocuments({
        willId: uploadWillId,
        file: uploadFile,
      });

      showToast("Legal document submitted successfully.");
      closeLegalDocumentUpload();
      await load();
    } catch (err) {
      setError(err.message || "Could not submit legal document.");
    } finally {
      setBusyId(null);
    }
  }


  const dmsTriggers = data?.dmsTriggers ?? [];
  const stats = data
    ? [
        { label: "Pending requests", value: data.requests.filter((r) => r.status === "PENDING").length },
        { label: "Active wills", value: data.activeWills.length },
        { label: "Triggers", value: dmsTriggers.length },
        { label: "Open cases", value: data.cases.filter((c) => c.status === "OPEN").length },
      ]
    : [];

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div>
          <h1 className={styles.title}>Executor Dashboard</h1>
          <p className={styles.subtitle}>
            Manage requests from Owners, active wills, Dead Man's Switch triggers and legal cases.
          </p>
        </div>
        {stats.length > 0 && !isLoading && (
          <dl className={styles.stats}>
            {stats.map((st) => (
              <div className={styles.stat} key={st.label}>
                <dd className={styles.statValue}>{st.value}</dd>
                <dt className={styles.statLabel}>{st.label}</dt>
              </div>
            ))}
          </dl>
        )}
      </header>

      {toast && <div className={styles.toast}>{toast}</div>}
      {isLoading && <p className={styles.info}>Loading data...</p>}
      {error && <p className={styles.errorBox}>{error}</p>}

      {data && !isLoading && (
        <div className={styles.grid}>
          <div className={styles.feeRow}>
            <section className={`${styles.card} ${styles.feeCard}`}>
              <h2 className={styles.cardTitle}>My Service Fee</h2>
              {editingFee ? (
                <div>
                  <input
                    className={styles.feeInput}
                    type="number"
                    min="0"
                    value={feeInput}
                    onChange={(e) => setFeeInput(e.target.value)}
                  />
                  <div className={styles.actionRow} style={{ marginTop: 10 }}>
                    <button
                      className={styles.secondaryBtn}
                      onClick={() => {
                        setEditingFee(false);
                        setFeeInput(fee?.fee != null ? String(fee.fee) : "");
                      }}
                    >
                      Cancel
                    </button>
                    <button className={styles.primaryBtn} disabled={savingFee} onClick={handleSaveFee}>
                      Save
                    </button>
                  </div>
                </div>
              ) : (
                <div>
                  <p className={styles.feeAmount}>{fee?.fee != null ? `${fee.fee.toLocaleString("vi-VN")} VND` : "Not configured"}</p>
                  <p className={styles.metaText}>
                    {fee?.updatedAt ? `Last updated ${formatDate(fee.updatedAt)}` : "This is the fee Owners see when hiring you"}
                  </p>
                  <div className={styles.feeActions}>
                    <button className={styles.secondaryBtn} onClick={() => setEditingFee(true)}>
                      Edit Fee
                    </button>
                    <EvaluationCsvActions pricing={pricing} variant="dark" />
                  </div>
                </div>
              )}
            </section>

            <ExecutorPricingCard pricing={pricing} />
          </div>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>
              Requests from Owner <span className={styles.count}>{data.requests.length}</span>
            </h2>
            {data.requests.length === 0 ? (
              <p className={styles.mutedText}>No pending requests</p>
            ) : (
              <ul className={styles.list}>
                {data.requests.map((req) => {
                  const deadline = req.status === "PENDING" ? formatDeadline(req.deadlineAt) : null;
                  return (
                    <li key={req.id} className={styles.listItemColumn}>
                      <div className={styles.listItem} style={{ border: "none", padding: 0 }}>
                        <div>
                          <strong>{req.ownerName}</strong>
                          <div className={styles.metaText}>
                            Sent {formatDate(req.sentAt)}
                          </div>
                          {deadline && (
                            <div className={deadline.overdue ? styles.deadlineOverdue : styles.metaText}>
                              {deadline.text}
                            </div>
                          )}
                        </div>
                        <div className={styles.itemRight}>
                          <Badge status={req.status} />
                          <button className={styles.linkBtn} onClick={() => toggleExpanded(req.id)}>
                            {expandedId === req.id ? "Hide Details" : "View Details"}
                          </button>
                        </div>
                      </div>

                      {expandedId === req.id && (
                        <div className={styles.detailPanel}>
                          <div className={styles.detailRow}>
                            <span>Will Title</span>
                            <span>{req.willTitle || "—"}</span>
                          </div>
                          <div className={styles.detailRow}>
                            <span>Email</span>
                            <span>{req.ownerEmail || "—"}</span>
                          </div>
                          <div className={styles.detailRow}>
                            <span>Phone</span>
                            <span>{req.ownerPhone || "—"}</span>
                          </div>
                          <div className={styles.detailRow}>
                            <span>Assets</span>
                            <span>{req.assetsCount ?? "—"}</span>
                          </div>
                          <div className={styles.detailRow}>
                            <span>Proposed Fee</span>
                            <span>{req.proposedFee ? `${req.proposedFee.toLocaleString("vi-VN")} VND` : "—"}</span>
                          </div>
                          <div style={{ marginTop: 12 }}>
                            <strong>Assets ({req.assets?.length ?? 0})</strong>
                            {(req.assets || []).length === 0 ? <p className={styles.metaText}>No assets assigned.</p> :
                              <ul style={{ paddingLeft: 18 }}>
                                {req.assets.map(a => <li key={a.willAssetId} style={{ marginTop: 8 }}>
                                  <strong>{a.assetName}</strong>
                                  <div className={styles.metaText}>Type: {a.assetType || "—"}{a.provider && a.provider !== a.assetType ? ` · ${a.provider}` : ""}</div>
                                  <div className={styles.metaText}>Beneficiary: {(a.beneficiaries || []).map(b => `${b.fullName}${b.relationship ? ` (${b.relationship})` : ""}`).join(", ") || "Not assigned"}</div>
                                </li>)}
                              </ul>}
                          </div>
                          <div style={{ marginTop: 12 }}>
                            <strong>Beneficiaries ({req.beneficiaries?.length ?? 0})</strong>
                            {(req.beneficiaries || []).map(b =>
                              <div className={styles.metaText} key={b.id}>{b.fullName}{b.relationship ? ` · ${b.relationship}` : ""}</div>)}
                          </div>
                          {req.message && <p className={styles.metaText}>"{req.message}"</p>}
                        </div>
                      )}

                      {req.status === "DECLINED" && (
                        <div className={styles.actionRow} style={{ marginTop: 8 }}>
                          <button
                            className={styles.dangerBtn}
                            onClick={() => startDeleteRequest(req)}
                          >
                            Delete
                          </button>
                        </div>
                      )}

                      {req.status === "PENDING" && (
                        <div className={styles.actionRow} style={{ marginTop: 8 }}>
                          <button
                            className={styles.primaryBtn}
                            disabled={busyId === req.id || deadline?.overdue}
                            onClick={() => handleRequest(req.id, true)}
                            title={deadline?.overdue ? "Response deadline has passed for this request" : undefined}
                          >
                            Accept
                          </button>
                          <button
                            className={styles.secondaryBtn}
                            disabled={busyId === req.id || deadline?.overdue}
                            onClick={() => handleRequest(req.id, false)}
                            title={deadline?.overdue ? "Response deadline has passed for this request" : undefined}
                          >
                            Decline
                          </button>
                        </div>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>
              Immediate Handover Requests <span className={styles.count}>{data.handoverRequests.length}</span>
            </h2>
            {data.handoverRequests.length === 0 ? (
              <p className={styles.mutedText}>No immediate handover requests</p>
            ) : (
              <ul className={styles.list}>
                {data.handoverRequests.map((hr) => {
                  const deadline = hr.status === "PENDING" ? formatDeadline(hr.deadlineAt) : null;
                  return (
                    <li key={hr.id} className={styles.listItemColumn}>
                      <div className={styles.listItem} style={{ border: "none", padding: 0 }}>
                        <div>
                          <strong>{hr.ownerName}</strong>
                          <div className={styles.metaText}>
                            {hr.willTitle} · Requested {formatDate(hr.requestedAt)}
                          </div>
                          {deadline && (
                            <div className={deadline.overdue ? styles.deadlineOverdue : styles.metaText}>
                              {deadline.text}
                            </div>
                          )}
                        </div>
                        <div className={styles.itemRight}>
                          <Badge status={hr.status} />
                          <button className={styles.linkBtn} onClick={() => toggleExpanded(hr.id)}>
                            {expandedId === hr.id ? "Hide Details" : "View Details"}
                          </button>
                        </div>
                      </div>

                      {expandedId === hr.id && (
                        <div className={styles.detailPanel}>
                          <p className={styles.metaText}>
                            The Owner wants to hand these assets over now, while still alive. Confirm only
                            once you've checked each beneficiary exists and is valid.
                          </p>
                          {(hr.assets || []).map((a) => (
                            <div className={styles.detailRow} key={a.id}>
                              <span>{a.name} ({a.type})</span>
                              <span>→ {a.beneficiaryName}</span>
                            </div>
                          ))}
                        </div>
                      )}

                      {hr.status === "PENDING" && (
                        <div className={styles.actionRow} style={{ marginTop: 8 }}>
                          <button
                            className={styles.primaryBtn}
                            disabled={busyId === hr.id || deadline?.overdue}
                            onClick={() => handleHandoverResponse(hr.id, true)}
                            title={deadline?.overdue ? "Response deadline has passed for this request" : undefined}
                          >
                            Confirm
                          </button>
                          <button
                            className={styles.secondaryBtn}
                            disabled={busyId === hr.id || deadline?.overdue}
                            onClick={() => handleHandoverResponse(hr.id, false)}
                            title={deadline?.overdue ? "Response deadline has passed for this request" : undefined}
                          >
                            Decline
                          </button>
                        </div>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>
              Active Wills <span className={styles.count}>{data.activeWills.length}</span>
            </h2>
            {data.activeWills.length === 0 ? (
              <p className={styles.mutedText}>No active wills yet</p>
            ) : (
              <ul className={styles.list}>
                {data.activeWills.map((will) => (
                  <li key={will.id} className={styles.listItemColumn}>
                    <div className={styles.listItem} style={{ border: "none", padding: 0 }}>
                      <div>
                        <strong>{will.willTitle}</strong>
                        <div className={styles.metaText}>Owner: {will.ownerName}</div>
                      </div>
                      <div className={styles.itemRight}>
                        <Badge status={will.status} />
                        <button className={styles.linkBtn} onClick={() => toggleExpanded(will.id)}>
                          {expandedId === will.id ? "Hide Details" : "View Details"}
                        </button>
                      </div>
                    </div>
                    {expandedId === will.id && (
                      <div className={styles.detailPanel}>
                        <p className={styles.metaText}>{will.description || "No description provided."}</p>
                        <div className={styles.detailRow}>
                          <span>Assets</span>
                          <span>{will.assetsCount ?? "—"}</span>
                        </div>
                        <div className={styles.detailRow}>
                          <span>Beneficiaries</span>
                          <span>{will.beneficiariesCount ?? "—"}</span>
                        </div>
                        <div className={styles.detailRow}>
                          <span>Dead Man's Switch</span>
                          <span>{will.dmsStatus || "—"}</span>
                        </div>
                        <div className={styles.detailRow}>
                          <span>Created</span>
                          <span>{formatDate(will.createdAt)}</span>
                        </div>
                        {will.assets && will.assets.length > 0 ? (
                          <>
                            <p className={styles.metaText} style={{ marginTop: 6 }}>
                              Asset breakdown
                            </p>
                            {will.assets.map((a) => (
                              <div className={styles.assetDetailCard} key={a.id}>
                                <div className={styles.detailRow}>
                                  <span>
                                    <strong>{a.name}</strong> ({a.type})
                                  </span>
                                  <span>{a.status || "—"}</span>
                                </div>
                                {a.description && (
                                  <p className={styles.metaText} style={{ margin: "4px 0" }}>
                                    {a.description}
                                  </p>
                                )}
                                <div className={styles.detailRow}>
                                  <span>Beneficiary</span>
                                  <span>{a.beneficiaryName || "Unassigned"}</span>
                                </div>
                                {a.beneficiaryRelationship && (
                                  <div className={styles.detailRow}>
                                    <span>Relationship</span>
                                    <span>{a.beneficiaryRelationship}</span>
                                  </div>
                                )}
                                {a.beneficiaryEmail && (
                                  <div className={styles.detailRow}>
                                    <span>Contact Email</span>
                                    <span>{a.beneficiaryEmail}</span>
                                  </div>
                                )}
                                {a.beneficiaryPhone && (
                                  <div className={styles.detailRow}>
                                    <span>Contact Phone</span>
                                    <span>{a.beneficiaryPhone}</span>
                                  </div>
                                )}
                              </div>
                            ))}
                          </>
                        ) : (
                          <p className={styles.metaText}>No individual asset details recorded for this will yet.</p>
                        )}
                      </div>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>



          <section className={styles.card}>
            <h2 className={styles.cardTitle}>
              Dead Man's Switch Triggers <span className={styles.count}>{dmsTriggers.length}</span>
            </h2>

            {dmsTriggers.length === 0 ? (
              <p className={styles.mutedText}>No triggers</p>
            ) : (
              <ul className={styles.list}>
                {dmsTriggers.map((trig) => (
                  <li
                    key={trig.assignmentId || trig.id}
                    className={styles.triggerCardItem}
                  >
                    <div>
                      <strong>
                        {trig.ownerName || "Unknown Owner"}
                      </strong>

                      <div className={styles.metaText}>
                        Will: {trig.willTitle || "—"}
                      </div>

                      <div className={styles.metaText}>
                        Triggered:{" "}
                        {trig.thresholdReachedAt
                          ? formatDate(trig.thresholdReachedAt)
                          : trig.triggeredAt
                            ? formatDate(trig.triggeredAt)
                            : "—"}
                      </div>

                      <div className={styles.metaText}>
                        Missed check-ins: {trig.missedCheckIns ?? "—"} /{" "}
                        {trig.maxMissedCheckIns ?? "—"}
                      </div>
                    </div>



                    <div className={styles.triggerActions}>
                      <Badge status={trig.status || "TRIGGERED"} />

                      {Number(trig.missedCheckIns ?? 0) >=
                        Number(trig.maxMissedCheckIns ?? Infinity) &&
                        trig.willId && (
                          <button
                            className={styles.triggerSubmitBtn}
                            disabled={busyId === trig.willId}
                            onClick={() => openLegalDocumentUpload(trig.willId)}
                          >
                            Submit Legal Document
                          </button>
                        )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>



          <section className={`${styles.card} ${styles.cardWide}`}>
            <h2 className={styles.cardTitle}>
              Legal Cases <span className={styles.count}>{data.cases.length}</span>
            </h2>
            {data.cases.length === 0 ? (
              <p className={styles.mutedText}>No cases opened yet</p>
            ) : (
              <ul className={styles.list}>
                {data.cases.map((c) => {
                  const overdue = c.deadlineAt && !c.profileVerified && new Date(c.deadlineAt) < new Date();
                  return (
                    <li key={c.id} className={styles.listItemColumn}>
                      <div className={styles.listItem} style={{ border: "none", padding: 0 }}>
                        <div>
                          <strong>Case for {c.ownerName}</strong>
                          <div className={styles.metaText}>
                            Will: {c.willTitle || "—"} · Submitted: {formatDate(c.submittedAt)}
                          </div>
                          {c.status === "OPEN" && (
                            <div className={overdue ? styles.deadlineOverdue : styles.metaText}>
                              {overdue ? "Overdue — " : "Processing deadline: "}
                              {formatDate(c.deadlineAt)}
                            </div>
                          )}
                        </div>
                        <div className={styles.itemRight}>
                          <Badge status={c.status} />
                          <button className={styles.linkBtn} onClick={() => toggleExpanded(c.id)}>
                            {expandedId === c.id ? "Hide Details" : "View Details"}
                          </button>
                        </div>
                      </div>

                      {expandedId === c.id && (
                        <div className={styles.detailPanel}>
                          <div className={styles.detailRow}>
                            <span>Will ID</span>
                            <span>{c.willId || "—"}</span>
                          </div>

                          <div className={styles.detailRow}>
                            <span>Will Title</span>
                            <span>{c.willTitle || "—"}</span>
                          </div>

                          <div className={styles.detailRow}>
                            <span>Owner</span>
                            <span>{c.ownerName || "—"}</span>
                          </div>

                          <div className={styles.detailRow}>
                            <span>Executor</span>
                            <span>{c.executorName || "—"}</span>
                          </div>

                          <div className={styles.detailRow}>
                            <span>Status</span>
                            <span>{c.status || "—"}</span>
                          </div>

                          <div className={styles.detailRow}>
                            <span>Submitted At</span>
                            <span>{formatDate(c.submittedAt)}</span>
                          </div>

                          <div className={styles.detailRow}>
                            <span>Reviewed At</span>
                            <span>{formatDate(c.reviewedAt)}</span>
                          </div>
                        </div>
                      )}

                      {c.status === "OPEN" && (
                        <div className={styles.actionRow} style={{ marginTop: 8 }}>
                          {!c.profileVerified ? (
                            <button
                              className={styles.primaryBtn}
                              disabled={busyId === c.id || overdue}
                              onClick={() => handleVerifyProfile(c.id)}
                              title={overdue ? "Deadline has passed for verifying this case" : undefined}
                            >
                              Verify Owner Profile
                            </button>
                          ) : (
                            <button
                              className={styles.primaryBtn}
                              disabled={busyId === c.id}
                              onClick={() => openUploadPopup(c.id)}
                            >
                              Submit Legal Documents
                            </button>
                          )}
                        </div>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </section>
        </div>
      )}

      {deleteConfirmReq && (
        <div className={styles.overlay} onClick={() => setDeleteConfirmReq(null)}>
          <div
            className={styles.modal}
            role="dialog"
            aria-modal="true"
            aria-label="Delete declined request"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className={styles.modalTitle}>Delete this request?</h2>
            <p className={styles.metaText}>
              You declined the request from <strong>{deleteConfirmReq.ownerName}</strong>
              {deleteConfirmReq.willTitle ? ` (${deleteConfirmReq.willTitle})` : ""}.
              Are you sure you want to delete it? It will be removed from your dashboard and this cannot be undone.
              You will need to enter your Security PIN to confirm.
            </p>
            <div className={styles.actionRow} style={{ justifyContent: "flex-end", marginTop: 16 }}>
              <button className={styles.secondaryBtn} onClick={() => setDeleteConfirmReq(null)}>
                Cancel
              </button>
              <button className={styles.dangerBtnSolid} onClick={continueDeleteToPin}>
                Yes, continue
              </button>
            </div>
          </div>
        </div>
      )}

      {deletePinReq && (
        <SecurityPinModal
          scope="EXECUTOR_REQUEST_DELETE"
          resourceId={deletePinReq.id}
          onVerified={handleDeleteVerified}
          onClose={() => setDeletePinReq(null)}
        />
      )}

      {uploadCaseId && (
        <div className={styles.overlay} onClick={closeUploadPopup}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Upload Legal Documents</h2>
            <p className={styles.metaText}>
              Attach the death certificate and identity verification documents for this case.
            </p>
            <label className={styles.uploadArea}>
              <input type="file" multiple onChange={handleFilesChosen} className={styles.hiddenFileInput} />
              {uploadFiles.length > 0 ? (
                <span>{uploadFiles.join(", ")}</span>
              ) : (
                <span>Upload file here</span>
              )}
            </label>
            <div className={styles.actionRow} style={{ justifyContent: "flex-end", marginTop: 16 }}>
              <button className={styles.secondaryBtn} disabled={busyId === uploadCaseId} onClick={closeUploadPopup}>
                Cancel Upload
              </button>
              <button className={styles.primaryBtn} disabled={busyId === uploadCaseId} onClick={handleConfirmUpload}>
                Confirm Upload
              </button>
            </div>
          </div>
        </div>
      )}

      {uploadWillId && (
        <div
          className={styles.overlay}
          onClick={closeLegalDocumentUpload}
        >
          <div
            className={styles.modal}
            onClick={(event) => event.stopPropagation()}
          >
            <h2 className={styles.modalTitle}>
              Submit Legal Document
            </h2>

            <p className={styles.metaText}>
              Choose one PDF, JPG, JPEG, or PNG file. Maximum size: 5 MB.
            </p>

            <label className={styles.uploadArea}>
              <input
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                className={styles.hiddenFileInput}
                onChange={handleLegalDocumentSelected}
              />

              <span>
                {uploadFile ? uploadFile.name : "Choose a document"}
              </span>
            </label>

            <div
              className={styles.actionRow}
              style={{ justifyContent: "flex-end", marginTop: 16 }}
            >
              <button
                className={styles.secondaryBtn}
                disabled={busyId === uploadWillId}
                onClick={closeLegalDocumentUpload}
              >
                Cancel
              </button>

              <button
                className={styles.primaryBtn}
                disabled={!uploadFile || busyId === uploadWillId}
                onClick={handleLegalDocumentSubmit}
              >
                {busyId === uploadWillId ? "Uploading..." : "Submit"}
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}

function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("en-US");
}
