import { useEffect, useState } from "react";
import { getPricingRules, updatePricingRules } from "../../api/pricingApi";
import styles from "./AdminPricingRules.module.css";

const FIELDS = [
  ["baseFee", "Base fee"],
  ["perExperienceYear", "Per year of experience (max 40)"],
  ["perStar", "Per rating star"],
  ["perCompletedCase", "Per completed case (max 200)"],
  ["winRateBonus", "Bonus at 100% win rate"],
  ["minFee", "Minimum fee"],
  ["maxFee", "Maximum fee"],
  ["roundTo", "Round to nearest"],
];

// Admin: edit the weights that produce each Executor's suggested fee.
export default function AdminPricingRules() {
  const [form, setForm] = useState(null);
  const [meta, setMeta] = useState(null);
  const [error, setError] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getPricingRules()
      .then((r) => {
        setForm(Object.fromEntries([...FIELDS.map(([k]) => [k, String(r[k])]), ["currency", r.currency]]));
        setMeta(r);
      })
      .catch((err) => setError(err.message || "Could not load pricing rules."));
  }, []);

  async function save(e) {
    e.preventDefault();
    setError("");
    setNote("");
    const body = { currency: form.currency.trim().toUpperCase() };
    for (const [k, label] of FIELDS) {
      const n = Number(form[k]);
      if (form[k].trim() === "" || !Number.isFinite(n) || n < 0) {
        return setError(`${label} must be a number, zero or more.`);
      }
      body[k] = n;
    }
    if (body.minFee > body.maxFee) return setError("Minimum fee cannot be larger than maximum fee.");
    setSaving(true);
    try {
      const r = await updatePricingRules(body);
      setMeta(r);
      setNote("Pricing rules saved. New suggestions use them immediately.");
    } catch (err) {
      setError(err.message || "Could not save the rules.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className={styles.card}>
      <h2 className={styles.title}>Executor pricing rules</h2>
      <p className={styles.muted}>
        Suggested fee = base + experience + stars + cases + win-rate bonus, limited to the min/max range.
        {meta?.isDefault && " Built-in defaults are in use until you save."}
      </p>
      {!form ? (
        <p className={styles.muted}>{error || "Loading..."}</p>
      ) : (
        <form onSubmit={save} className={styles.form}>
          <div className={styles.grid}>
            {FIELDS.map(([key, label]) => (
              <label key={key} className={styles.field}>
                <span>{label}</span>
                <input
                  type="number"
                  min="0"
                  value={form[key]}
                  onChange={(e) => setForm((f) => ({ ...f, [key]: e.target.value }))}
                />
              </label>
            ))}
            <label className={styles.field}>
              <span>Currency</span>
              <input
                maxLength={3}
                value={form.currency}
                onChange={(e) => setForm((f) => ({ ...f, currency: e.target.value }))}
              />
            </label>
          </div>
          {error && <p role="alert" className={styles.error}>{error}</p>}
          {note && <p role="status" className={styles.success}>{note}</p>}
          <button className={styles.btn} disabled={saving}>{saving ? "Saving..." : "Save rules"}</button>
        </form>
      )}
    </section>
  );
}
