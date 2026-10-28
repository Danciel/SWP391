import { useState } from "react";
import { downloadEvaluationTemplate } from "../../api/pricingApi";
import styles from "./ExecutorPricingCard.module.css";

const money = (n, c = "VND") =>
  `${Number(n || 0).toLocaleString("vi-VN")} ${c}`;

// Upload CSV + Download template. Rendered right next to "Edit Fee".
export function EvaluationCsvActions({ pricing, variant }) {
  const { fileRef, busy, handleFile } = pricing;
  const btn = variant === "dark" ? styles.ghostBtn : styles.secondaryBtn;
  return (
    <>
      <input ref={fileRef} type="file" accept=".csv,text/csv" hidden onChange={handleFile} />
      <button type="button" className={btn} disabled={busy} onClick={() => fileRef.current?.click()}>
        {busy ? "Working..." : "Upload CSV"}
      </button>
      <button type="button" className={btn} onClick={downloadEvaluationTemplate}>
        Download template
      </button>
    </>
  );
}

// Suggested price card. The breakdown table only appears after pressing "Details".
export default function ExecutorPricingCard({ pricing }) {
  const { data, error, note, busy, useSuggested } = pricing;
  const [showDetails, setShowDetails] = useState(false);

  const diff = data?.feeDifferencePercent;
  const position =
    data?.feePosition === "ABOVE" ? `Your fee is ${Math.abs(diff).toFixed(1)}% above the suggestion.`
    : data?.feePosition === "BELOW" ? `Your fee is ${Math.abs(diff).toFixed(1)}% below the suggestion.`
    : data?.feePosition === "AT" ? "Your fee is in line with the suggestion."
    : "Set your fee to compare it with the suggestion.";

  return (
    <section className={styles.card}>
      <h2 className={styles.title}>Suggested fee</h2>

      {error && <p role="alert" className={styles.error}>{error}</p>}
      {note && <p role="status" className={styles.success}>{note}</p>}

      {data && (
        <>
          <div className={styles.summary}>
            <div>
              <span className={styles.label}>Suggested</span>
              <strong className={styles.big}>{data.suggestedFee != null ? money(data.suggestedFee, data.currency) : "—"}</strong>
            </div>
            <div>
              <span className={styles.label}>Your fee</span>
              <strong className={styles.big}>{data.currentFee > 0 ? money(data.currentFee, data.currency) : "Not set"}</strong>
            </div>
          </div>
          <p className={`${styles.position} ${styles[data.feePosition] || ""}`}>{position}</p>

          <div className={styles.row}>
            <button
              type="button"
              className={styles.secondaryBtn}
              disabled={busy || data.suggestedFee == null || data.feePosition === "AT"}
              onClick={useSuggested}
            >
              Use suggested price
            </button>
            <button
              type="button"
              className={styles.linkBtn}
              aria-expanded={showDetails}
              onClick={() => setShowDetails((v) => !v)}
            >
              {showDetails ? "Hide details" : "Details"}
            </button>
          </div>

          {showDetails && (
            <>
              <ul className={styles.parts}>
                {data.components.map((c) => (
                  <li key={c.label}>
                    <span>{c.label} <em>{c.detail}</em></span>
                    <span>{money(c.amount, data.currency)}</span>
                  </li>
                ))}
              </ul>
              {data.clamped && (
                <p className={styles.muted}>
                  The total was limited to the allowed range {money(data.minFee, data.currency)} – {money(data.maxFee, data.currency)}.
                </p>
              )}
              <p className={styles.muted}>
                {data.evaluationSource === "CSV"
                  ? `Based on your CSV (updated ${new Date(data.evaluationUpdatedAt).toLocaleDateString("en-GB")}).`
                  : "Based on your profile only. Upload a CSV for a more accurate suggestion."}
              </p>
            </>
          )}
        </>
      )}
    </section>
  );
}
