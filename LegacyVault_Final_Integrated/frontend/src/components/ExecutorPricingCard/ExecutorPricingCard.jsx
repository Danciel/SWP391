import { useCallback, useEffect, useRef, useState } from "react";
import { getMyPricingSuggestion, uploadEvaluationCsv, downloadEvaluationTemplate } from "../../api/pricingApi";
import { updateMyExecutorPricing } from "../../api/vaultApi";
import styles from "./ExecutorPricingCard.module.css";

const money = (n, c = "VND") =>
  `${Number(n || 0).toLocaleString("vi-VN")} ${c}`;

// Executor dashboard: evaluation CSV + suggested price + one-click apply.
export default function ExecutorPricingCard({ onFeeUpdated }) {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const fileRef = useRef(null);

  const load = useCallback(async () => {
    try {
      setData(await getMyPricingSuggestion());
      setError("");
    } catch (err) {
      setError(err.message || "Could not load the pricing suggestion.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleFile(e) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    setBusy(true);
    setError("");
    setNote("");
    try {
      const result = await uploadEvaluationCsv(file);
      setData(result.suggestion);
      setNote("Evaluation file accepted. The suggestion was updated.");
    } catch (err) {
      setError(err.message || "Could not read the CSV file.");
    } finally {
      setBusy(false);
    }
  }

  async function useSuggested() {
    if (!data?.suggestedFee) return;
    setBusy(true);
    setError("");
    setNote("");
    try {
      const updated = await updateMyExecutorPricing({ serviceFee: data.suggestedFee, currency: data.currency });
      onFeeUpdated?.(updated);
      await load();
      setNote("Your fee now matches the suggestion.");
    } catch (err) {
      setError(err.message || "Could not update your fee.");
    } finally {
      setBusy(false);
    }
  }

  const diff = data?.feeDifferencePercent;
  const position =
    data?.feePosition === "ABOVE" ? `Your fee is ${Math.abs(diff).toFixed(1)}% above the suggestion.`
    : data?.feePosition === "BELOW" ? `Your fee is ${Math.abs(diff).toFixed(1)}% below the suggestion.`
    : data?.feePosition === "AT" ? "Your fee is in line with the suggestion."
    : "Set your fee to compare it with the suggestion.";

  return (
    <section className={styles.card}>
      <h2 className={styles.title}>Evaluation & suggested price</h2>
      <p className={styles.muted}>
        Upload a CSV with your experience, rating, cases and wins. Owners see these numbers next to your price.
      </p>

      <div className={styles.row}>
        <input ref={fileRef} type="file" accept=".csv,text/csv" hidden onChange={handleFile} />
        <button type="button" className={styles.primaryBtn} disabled={busy} onClick={() => fileRef.current?.click()}>
          {busy ? "Working..." : "Upload evaluation CSV"}
        </button>
        <button type="button" className={styles.secondaryBtn} onClick={downloadEvaluationTemplate}>
          Download template
        </button>
      </div>

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
          <p className={styles.muted}>{position}</p>

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

          <button
            type="button"
            className={styles.secondaryBtn}
            disabled={busy || data.suggestedFee == null || data.feePosition === "AT"}
            onClick={useSuggested}
          >
            Use suggested price
          </button>
        </>
      )}
    </section>
  );
}
