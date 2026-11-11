import { useState } from "react";
import styles from "./HandoverMinutes.module.css";

const STATUS_LABEL = {
  PENDING_SIGNATURES: "Waiting for signatures",
  FULLY_SIGNED: "Fully signed",
  VOID: "Void (handover cancelled)",
};

const ROLE_LABEL = {
  OWNER: "Owner",
  EXECUTOR: "Executor",
  BENEFICIARY: "Beneficiary",
};

const fmt = (iso) => (iso ? new Date(iso).toLocaleString() : "");

/**
 * Handover minutes (C1-C3): what is being handed over, the SHA-256 that is
 * signed, who has signed, and the Sign button.
 *
 * Props
 *  minutes         response of GET /handover/{id}/minutes (or the beneficiary one)
 *  viewer          EXECUTOR or BENEFICIARY: the Sign button shows while THEIR signature is pending
 *  confirmText     sentence shown next to the "I confirm" checkbox
 *  canSign         false hides nothing, it only disables the button
 *  signHint        why signing is not possible yet (shown under the button)
 *  signLabel       button text
 *  signBusy        request in progress
 *  onSign          () => Promise|void
 *  onDownloadPdf   optional; Owner / Executor only (the backend refuses beneficiaries)
 *  pdfBusy         PDF download in progress
 *  error           error text to show
 */
export default function HandoverMinutes({
  minutes,
  viewer, // "EXECUTOR" | "BENEFICIARY": only the viewer's own signature counts for the Sign button
  confirmText,
  canSign = true,
  signHint = "",
  signLabel = "Sign minutes",
  signBusy = false,
  onSign,
  onDownloadPdf,
  pdfBusy = false,
  error = "",
}) {
  const [confirmed, setConfirmed] = useState(false);

  if (!minutes) return null;

  const items = minutes.content?.items || [];
  const signatures = minutes.signatures || [];
  const pending = signatures.some(
    (s) => (!viewer || s.role === viewer) && s.status !== "SIGNED"
  );
  const isVoid = minutes.status === "VOID";
  const showSign = Boolean(onSign) && pending && !isVoid;

  return (
    <div className={styles.box}>
      <div className={styles.head}>
        <strong>Handover minutes</strong>
        <span
          className={
            minutes.status === "FULLY_SIGNED"
              ? styles.tagOk
              : isVoid
                ? styles.tagBad
                : styles.tagWait
          }
        >
          {STATUS_LABEL[minutes.status] || minutes.status}
        </span>
      </div>

      {minutes.content && (
        <p className={styles.meta}>
          {minutes.content.willTitle} · Owner {minutes.content.ownerName} · Executor{" "}
          {minutes.content.executorName}
        </p>
      )}

      {items.length > 0 && (
        <ul className={styles.items}>
          {items.map((it) => (
            <li key={it.assetAssignmentId}>
              <span>
                {it.assetName} <span className={styles.faint}>({it.assetType})</span>
              </span>
              <span>
                → {it.beneficiaryName}
                {it.relationship ? ` · ${it.relationship}` : ""} · {it.allocationPercentage}%
              </span>
            </li>
          ))}
        </ul>
      )}

      <div className={styles.hashRow}>
        <span className={styles.faint}>Content SHA-256</span>
        <code>{minutes.contentSha256}</code>
      </div>

      <div className={styles.sigs}>
        {signatures.map((sg) => (
          <div key={sg.id} className={styles.sigRow}>
            <span>
              {ROLE_LABEL[sg.role] || sg.role}
              {sg.signerName ? ` · ${sg.signerName}` : ""}
            </span>
            {sg.status === "SIGNED" ? (
              <span className={styles.okText}>✓ Signed {fmt(sg.signedAt)}</span>
            ) : (
              <span className={styles.faint}>Waiting for signature</span>
            )}
          </div>
        ))}
      </div>

      {minutes.fullySignedAt && (
        <p className={styles.okText}>Everyone signed on {fmt(minutes.fullySignedAt)}.</p>
      )}

      {showSign && (
        <div className={styles.signBox}>
          <label className={styles.confirm}>
            <input
              type="checkbox"
              checked={confirmed}
              disabled={!canSign || signBusy}
              onChange={(e) => setConfirmed(e.target.checked)}
            />
            <span>{confirmText || "I have read these minutes and I confirm they are correct."}</span>
          </label>
          <button
            type="button"
            className={styles.primaryBtn}
            disabled={!canSign || !confirmed || signBusy}
            onClick={async () => {
              await onSign();
              setConfirmed(false);
            }}
          >
            {signBusy ? "Signing..." : signLabel}
          </button>
          {!canSign && signHint && <p className={styles.hint}>{signHint}</p>}
        </div>
      )}

      {onDownloadPdf && (
        <button
          type="button"
          className={styles.secondaryBtn}
          disabled={pdfBusy}
          onClick={onDownloadPdf}
        >
          {pdfBusy ? "Downloading..." : "Download minutes (PDF)"}
        </button>
      )}

      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}
    </div>
  );
}
