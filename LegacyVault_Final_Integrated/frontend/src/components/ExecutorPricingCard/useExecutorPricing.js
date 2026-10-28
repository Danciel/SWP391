import { useCallback, useEffect, useRef, useState } from "react";
import { getMyPricingSuggestion, uploadEvaluationCsv } from "../../api/pricingApi";
import { updateMyExecutorPricing } from "../../api/vaultApi";

// Shared state for the "Evaluation & suggested price" feature.
// The upload button (next to Edit Fee) and the suggestion card use the same instance.
export default function useExecutorPricing({ onFeeUpdated } = {}) {
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
      const updated = await updateMyExecutorPricing({
        serviceFee: data.suggestedFee,
        currency: data.currency,
      });
      onFeeUpdated?.(updated);
      await load();
      setNote("Your fee now matches the suggestion.");
    } catch (err) {
      setError(err.message || "Could not update your fee.");
    } finally {
      setBusy(false);
    }
  }

  return { data, error, note, busy, fileRef, handleFile, useSuggested, load };
}
