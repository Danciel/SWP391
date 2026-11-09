import styles from "./ExecutorFeeInsight.module.css";

function money(amount, currency = "VND") {
  const n = Number(amount);
  if (!Number.isFinite(n)) return "—";
  try {
    return new Intl.NumberFormat("vi-VN", { style: "currency", currency, maximumFractionDigits: 0 }).format(n);
  } catch {
    return `${n.toLocaleString("vi-VN")} ${currency}`;
  }
}

// Shows the Executor's asking price against the system suggestion, so an Owner
// can see whether the Executor is asking more or less than the data supports.
export default function ExecutorFeeInsight({ executor }) {
  if (!executor) return null;

  const currency = executor.feeCurrency || executor.currency || "VND";
  const asking = executor.feeAmount ?? executor.serviceFee;
  const {
    suggestedFee, feeDifferencePercent, feePosition,
    evaluationSource, evalRating, totalCases, wonCases, winRatePercent,
  } = executor;

  const hasSuggestion = suggestedFee != null;
  const pct = feeDifferencePercent != null ? Math.abs(feeDifferencePercent).toFixed(1) : null;

  let badge = null;
  if (feePosition === "ABOVE") badge = { cls: styles.above, text: `▲ ${pct}% above suggestion` };
  else if (feePosition === "BELOW") badge = { cls: styles.below, text: `▼ ${pct}% below suggestion` };
  else if (feePosition === "AT") badge = { cls: styles.at, text: "In line with suggestion" };

  const fromCsv = evaluationSource === "CSV";

  return (
    <div className={styles.box}>
      {hasSuggestion ? (
        <p className={styles.line}>
          System suggestion: <strong>{money(suggestedFee, currency)}</strong>
          {asking > 0 && <> · Asking: <strong>{money(asking, currency)}</strong></>}
          {badge && <span className={`${styles.badge} ${badge.cls}`}>{badge.text}</span>}
        </p>
      ) : (
        <p className={styles.line}>No system suggestion available yet.</p>
      )}

      {fromCsv ? (
        <p className={styles.meta}>
          Self-reported (CSV): ⭐ {Number(evalRating ?? 0).toFixed(1)}
          {totalCases != null && <> · {totalCases} cases</>}
          {wonCases != null && <> · {wonCases} won{winRatePercent != null ? ` (${winRatePercent}%)` : ""}</>}
        </p>
      ) : (
        <p className={styles.meta}>No evaluation file uploaded; suggestion uses profile data only.</p>
      )}

      {feePosition === "ABOVE" && (
        <p className={styles.hint}>
          A higher price can reflect specialization or availability. Compare reviews and cases before you decide.
        </p>
      )}
    </div>
  );
}
