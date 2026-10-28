import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { getWallet, topUpWallet } from "../../api/vaultApi";
import { fmtDate } from "../../utils/willHelpers";
import styles from "./Wallet.module.css";

const money = (n, cur = "VND") => `${Number(n || 0).toLocaleString("en-US")} ${cur}`;

export default function Wallet() {
  const { user } = useAuth();
  const canTopUp = user?.role?.toUpperCase() === "USER";
  const [wallet, setWallet] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [amount, setAmount] = useState("");
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState("");

  async function load() {
    setLoading(true);
    setError("");
    try {
      setWallet(await getWallet());
    } catch (err) {
      setError(err.message || "Could not load wallet");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function handleTopUp(e) {
    e.preventDefault();
    setDone("");
    setError("");
    const value = Number(amount);
    if (!Number.isFinite(value) || value < 1 || value > 100000000) {
      setError("Enter an amount between 1 and 100,000,000.");
      return;
    }
    setBusy(true);
    try {
      setWallet(await topUpWallet(value));
      setAmount("");
      setDone("Mock top-up recorded. No real bank is connected.");
    } catch (err) {
      setError(err.message || "Top-up failed");
    } finally {
      setBusy(false);
    }
  }

  if (loading) return <p className={styles.muted}>Loading...</p>;

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Wallet</h1>
      <p className={styles.muted}>Simulated balance used to pay executor fees. No real bank is connected.</p>

      {error && <p role="alert" className={styles.error}>{error}</p>}

      {wallet && (
        <>
          <div className={styles.cards}>
            <div className={styles.card}>
              <span className={styles.cardLabel}>Available balance</span>
              <strong className={styles.cardValue}>
                {money(wallet.balance, wallet.currency)}
              </strong>
            </div>

            <div className={`${styles.card} ${styles.cardHeld}`}>
              <span className={styles.cardLabel}>Held for handover</span>
              <strong className={styles.cardValue}>
                {money(wallet.heldAmount, wallet.currency)}
              </strong>
              <p className={styles.cardNote}>
                Locked until the handover is completed.
              </p>
            </div>

            <div className={styles.card}>
              <span className={styles.cardLabel}>Pending incoming</span>
              <strong className={styles.cardValue}>
                {money(wallet.pendingIncoming, wallet.currency)}
              </strong>
              <p className={styles.cardNote}>
                Funds held for you as executor, awaiting release.
              </p>
            </div>
          </div>

          {canTopUp && (
            <>
              <form className={styles.topup} onSubmit={handleTopUp}>
                <input
                  className={styles.input}
                  type="number"
                  min="1"
                  max="100000000"
                  placeholder="Amount"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                />
                <button className={styles.primaryBtn} disabled={busy}>
                  {busy ? "Processing..." : "Mock Top-up"}
                </button>
              </form>

              {done && <p role="status" className={styles.success}>{done}</p>}
            </>
          )}

          <h2 className={styles.sectionTitle}>Transactions</h2>
          {(wallet.transactions || []).length === 0 ? (
            <p className={styles.muted}>No transactions yet.</p>
          ) : (
            <table className={styles.table}>
              <thead>
                <tr><th>Date</th><th>Type</th><th>Amount</th><th>Balance after</th><th>Note</th></tr>
              </thead>
              <tbody>
                {wallet.transactions.map((t) => (
                  <tr key={t.id}>
                    <td>{fmtDate(t.createdAt, true)}</td>
                    <td>{t.type.replaceAll("_", " ")}</td>
                    <td>{money(t.amount, wallet.currency)}</td>
                    <td>{money(t.balanceAfter, wallet.currency)}</td>
                    <td>{t.note || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </div>
  );
}
