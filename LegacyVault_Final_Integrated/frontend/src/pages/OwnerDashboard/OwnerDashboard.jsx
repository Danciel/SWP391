import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getOwnerDashboard, cancelHandover } from "../../api/ownerApi";
import logo from "../../assets/logo.png";
import styles from "./OwnerDashboard.module.css";

const STATUS_LABEL = {
  ACTIVE: "Active",
  PENDING_VERIFICATION: "Pending Verification",
  LOCKED: "Locked",
};

export default function OwnerDashboard() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const [data, setData] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const result = await getOwnerDashboard(user?.id);
      setData(result);
    } catch (err) {
      setError(err.message || "Could not load vault data");
    } finally {
      setIsLoading(false);
    }
  }, [user]);

  useEffect(() => {
    load();
  }, [load]);

  function handleLogout() {
    logout();
    navigate("/login");
  }

  function showToast(msg) {
    setToast(msg);
    setTimeout(() => setToast(""), 2500);
  }

  async function handleCancelHandover() {
    if (!data?.deadManSwitch?.willId) return;
    setBusy(true);
    try {
      await cancelHandover({ userId: user?.id, willId: data.deadManSwitch.willId });
      showToast("Handover cancelled — welcome back");
      await load();
    } catch (err) {
      setError(err.message || "Could not cancel handover");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <img src={logo} alt="LegacyVault" className={styles.logo} />
          <div>
            <h1 className={styles.title}>Owner Dashboard</h1>
            <p className={styles.welcome}>
              Welcome, {user?.fullName || "Vault Owner"}
            </p>
          </div>
        </div>
        <button className={styles.logoutBtn} onClick={handleLogout}>
          Log out
        </button>
      </header>

      {toast && <div className={styles.toast}>{toast}</div>}
      {isLoading && <p className={styles.info}>Loading data...</p>}
      {error && <p className={styles.errorBox}>{error}</p>}

      {data && !isLoading && (
        <div className={styles.grid}>
          <section className={styles.card}>
            <h2 className={styles.cardTitle}>Vault Status</h2>
            <span className={`${styles.badge} ${styles[data.vaultStatus]}`}>
              {STATUS_LABEL[data.vaultStatus] || data.vaultStatus}
            </span>
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>Dead Man's Switch</h2>
            {data.deadManSwitch.enabled ? (
              <ul className={styles.list}>
                <li>Check-in cycle: {data.deadManSwitch.checkInCycleDays} days</li>
                <li>
                  Last check-in:{" "}
                  {formatDate(data.deadManSwitch.lastCheckIn)}
                </li>
                <li>
                  Next check-in due:{" "}
                  {formatDate(data.deadManSwitch.nextCheckInDue)}
                </li>
                <li>
                  Missed count: {data.deadManSwitch.missedCount}/
                  {data.deadManSwitch.maxMissed}
                </li>
              </ul>
            ) : (
              <p className={styles.mutedText}>Dead Man's Switch not set up yet</p>
            )}

            {data.deadManSwitch.enabled && data.deadManSwitch.caseOpen && (
              <div className={styles.warningBox}>
                <p>
                  Your Dead Man's Switch has triggered and a handover case is now open with
                  your Executor. If this is a mistake, you can cancel it below.
                </p>
                <button className={styles.primaryBtn} disabled={busy} onClick={handleCancelHandover}>
                  I'm Still Active / Cancel Handover
                </button>
              </div>
            )}
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>
              Digital Assets ({data.assets.length})
            </h2>
            {data.assets.length === 0 ? (
              <p className={styles.mutedText}>No assets yet</p>
            ) : (
              <ul className={styles.list}>
                {data.assets.map((asset) => (
                  <li key={asset.id}>
                    <strong>{asset.name}</strong> — {asset.type} → {asset.beneficiary}
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>Executor & Beneficiary</h2>
            <p className={styles.subheading}>Digital Executor</p>
            {data.executors.length === 0 ? (
              <p className={styles.mutedText}>Not assigned yet</p>
            ) : (
              <ul className={styles.list}>
                {data.executors.map((ex) => (
                  <li key={ex.id}>{ex.name} ({ex.status})</li>
                ))}
              </ul>
            )}
            <p className={styles.subheading}>Beneficiary</p>
            {data.beneficiaries.length === 0 ? (
              <p className={styles.mutedText}>Not assigned yet</p>
            ) : (
              <ul className={styles.list}>
                {data.beneficiaries.map((b) => (
                  <li key={b.id}>
                    {b.name} — {b.assetsAssigned} assets
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className={`${styles.card} ${styles.cardWide}`}>
            <h2 className={styles.cardTitle}>Recent Activity</h2>
            <ul className={styles.list}>
              {data.recentActivity.map((log) => (
                <li key={log.id}>
                  {formatDate(log.time)} — {log.action}
                </li>
              ))}
            </ul>
          </section>
        </div>
      )}
    </div>
  );
}

function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("en-US");
}
