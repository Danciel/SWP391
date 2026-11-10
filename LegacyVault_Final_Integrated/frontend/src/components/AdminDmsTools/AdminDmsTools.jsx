import { useCallback, useEffect, useState } from "react";
import { forceTriggerDms, listAdminDms, runDmsEvaluation } from "../../api/adminApi";
import styles from "./AdminDmsTools.module.css";

const STATUS_LABEL = {
  WAITING: "Waiting",
  ACTIVE: "Active",
  TRIGGERED: "Triggered",
  // Old name of TRIGGERED, kept only so an old row never shows a raw code.
  THRESHOLD_REACHED: "Triggered",
};

function formatDate(iso) {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? "—" : d.toLocaleString("en-US");
}

// Admin: see every DMS and force the trigger when a demo or a support case needs it.
export default function AdminDmsTools() {
  const [rows, setRows] = useState(null);
  const [error, setError] = useState("");
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(null);
  const [openWill, setOpenWill] = useState(null);
  const [reason, setReason] = useState("");

  const load = useCallback(async () => {
    try {
      const data = await listAdminDms();
      setRows(Array.isArray(data) ? data : []);
      setError("");
    } catch (err) {
      setError(err.message || "Could not load the DMS list.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleForce(willId) {
    setBusy(willId);
    setError("");
    setNote("");
    try {
      const r = await forceTriggerDms(willId, reason.trim());
      setNote(r?.message || "DMS triggered.");
      setOpenWill(null);
      setReason("");
      await load();
    } catch (err) {
      setError(err.message || "Could not trigger the DMS.");
    } finally {
      setBusy(null);
    }
  }

  async function handleEvaluate() {
    setBusy("evaluate");
    setError("");
    setNote("");
    try {
      await runDmsEvaluation();
      setNote("DMS evaluation finished.");
      await load();
    } catch (err) {
      setError(err.message || "Could not run the evaluation.");
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className={`${styles.card} ${styles.cardWide}`}>
      <div className={styles.head}>
        <h2 className={styles.cardTitle}>
          Dead Man&apos;s Switch <span className={styles.count}>{rows ? rows.length : 0}</span>
        </h2>
        <button className={styles.secondaryBtn} disabled={busy === "evaluate"} onClick={handleEvaluate}>
          {busy === "evaluate" ? "Running..." : "Run evaluation now"}
        </button>
      </div>

      <p className={styles.metaText}>
        Forcing a trigger only tells the Executor to start. It does not mark the Owner deceased and
        does not start a handover: the Executor still needs a death certificate approved by Legal Verify.
      </p>

      {error && <p className={styles.error}>{error}</p>}
      {note && <p className={styles.ok}>{note}</p>}

      {rows && rows.length === 0 && <p className={styles.metaText}>No DMS configured yet</p>}

      <ul className={styles.list}>
        {(rows || []).map((r) => {
          const triggered = r.status === "TRIGGERED" || r.status === "THRESHOLD_REACHED";
          const canForce = r.enabled && !triggered && r.willStatus === "ACTIVE";
          return (
            <li key={r.willId} className={styles.item}>
              <div className={styles.row}>
                <div>
                  <strong>{r.willTitle}</strong>
                  <div className={styles.metaText}>
                    Owner: {r.ownerName || "—"} · Will {r.willStatus} · Missed {r.missedCheckIns}/
                    {r.maxMissedCheckIns} · Next check-in {formatDate(r.nextCheckInAt)}
                  </div>
                </div>
                <div className={styles.right}>
                  <span className={`${styles.badge} ${triggered ? styles.badgeTriggered : ""}`}>
                    {r.enabled ? STATUS_LABEL[r.status] || r.status : "Disabled"}
                  </span>
                  {canForce && openWill !== r.willId && (
                    <button
                      className={styles.dangerBtn}
                      onClick={() => {
                        setOpenWill(r.willId);
                        setReason("");
                      }}
                    >
                      Force trigger
                    </button>
                  )}
                </div>
              </div>

              {openWill === r.willId && (
                <div className={styles.reasonRow}>
                  <input
                    className={styles.input}
                    placeholder="Reason (required, kept in the audit log)"
                    value={reason}
                    maxLength={500}
                    onChange={(e) => setReason(e.target.value)}
                  />
                  <button
                    className={styles.dangerBtn}
                    disabled={busy === r.willId || reason.trim().length < 3}
                    onClick={() => handleForce(r.willId)}
                  >
                    {busy === r.willId ? "Triggering..." : "Confirm trigger"}
                  </button>
                  <button className={styles.secondaryBtn} onClick={() => setOpenWill(null)}>
                    Cancel
                  </button>
                </div>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
