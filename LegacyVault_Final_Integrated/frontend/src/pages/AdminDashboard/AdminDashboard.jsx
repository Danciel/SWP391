import { useCallback, useEffect, useState } from "react";
import { getAdminDashboard, setAccountStatus, setAccountRole } from "../../api/adminApi";
import styles from "./AdminDashboard.module.css";

const ROLE_LABEL = {
  OWNER: "Owner",
  EXECUTOR: "Executor",
  LEGAL_VERIFIER: "Legal Verifier",
  ADMIN: "Admin",
};

const ROLE_OPTIONS = Object.keys(ROLE_LABEL);

const CASE_STATUS_LABEL = {
  AWAITING_DOCUMENTS: "Awaiting Documents",
  PENDING_REVIEW: "Pending Review",
  APPROVED: "Approved",
  REJECTED: "Rejected",
};

function AccountBadge({ status }) {
  return (
    <span className={`${styles.badge} ${status === "ACTIVE" ? styles.ACTIVE : styles.SUSPENDED}`}>
      {status === "ACTIVE" ? "Active" : "Suspended"}
    </span>
  );
}

function CaseBadge({ status }) {
  return (
    <span className={`${styles.badge} ${styles[status] || ""}`}>
      {CASE_STATUS_LABEL[status] || status}
    </span>
  );
}

export default function AdminDashboard() {
  const [data, setData] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);
  const [toast, setToast] = useState("");
  const [roleBusyId, setRoleBusyId] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const result = await getAdminDashboard();
      setData(result);
    } catch (err) {
      setError(err.message || "Could not load admin data");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function showToast(msg) {
    setToast(msg);
    setTimeout(() => setToast(""), 2500);
  }

  async function handleToggleStatus(account) {
    setBusyId(account.id);
    try {
      const nextStatus = account.status === "ACTIVE" ? "SUSPENDED" : "ACTIVE";
      await setAccountStatus({ accountId: account.id, status: nextStatus });
      showToast(`${account.name} is now ${nextStatus.toLowerCase()}`);
      await load();
    } catch (err) {
      setError(err.message || "Could not update account");
    } finally {
      setBusyId(null);
    }
  }

  async function handleRoleChange(account, nextRole) {
    if (nextRole === account.role) return;
    setRoleBusyId(account.id);
    try {
      await setAccountRole({ accountId: account.id, role: nextRole });
      showToast(`${account.name}'s role is now ${ROLE_LABEL[nextRole]}`);
      await load();
    } catch (err) {
      setError(err.message || "Could not update role");
    } finally {
      setRoleBusyId(null);
    }
  }

  const totalPaid = data?.payments.reduce((sum, p) => sum + p.amount, 0) || 0;

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>Admin Dashboard</h1>
        <p className={styles.subtitle}>
          Manage accounts (Owner / Executor / Legal Verifier), review cases, payments and the audit log.
        </p>
      </header>

      {toast && <div className={styles.toast}>{toast}</div>}
      {isLoading && <p className={styles.info}>Loading data...</p>}
      {error && <p className={styles.errorBox}>{error}</p>}

      {data && !isLoading && (
        <div className={styles.grid}>
          <section className={`${styles.card} ${styles.cardWide}`}>
            <h2 className={styles.cardTitle}>Accounts ({data.accounts.length})</h2>
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Role</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {data.accounts.map((acc) => (
                    <tr key={acc.id}>
                      <td>{acc.name}</td>
                      <td>
                        <select
                          className={styles.roleSelect}
                          value={acc.role}
                          disabled={roleBusyId === acc.id}
                          onChange={(e) => handleRoleChange(acc, e.target.value)}
                        >
                          {ROLE_OPTIONS.map((role) => (
                            <option key={role} value={role}>
                              {ROLE_LABEL[role]}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <AccountBadge status={acc.status} />
                      </td>
                      <td className={styles.actionCell}>
                        <button
                          className={
                            acc.status === "ACTIVE" ? styles.dangerBtn : styles.primaryBtn
                          }
                          disabled={busyId === acc.id}
                          onClick={() => handleToggleStatus(acc)}
                        >
                          {acc.status === "ACTIVE" ? "Suspend" : "Reactivate"}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>Flagged Cases ({data.cases.length})</h2>
            <p className={styles.mutedText}>
              Only cases a Legal Verifier has flagged as suspected fraud appear here — regular executor cases are not
              visible to Admin.
            </p>
            {data.cases.length === 0 ? (
              <p className={styles.mutedText}>No flagged cases</p>
            ) : (
              <ul className={styles.list}>
                {data.cases.map((c) => (
                  <li key={c.id} className={styles.listItem}>
                    <div>
                      <strong>{c.ownerName}</strong>
                      <div className={styles.metaText}>Executor: {c.executorName}</div>
                      {c.flagReason && (
                        <div className={styles.flagReasonText}>Flag reason: {c.flagReason}</div>
                      )}
                    </div>
                    <CaseBadge status={c.status} />
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>
              Payments — ${totalPaid.toFixed(2)} total
            </h2>
            {data.payments.length === 0 ? (
              <p className={styles.mutedText}>No payments recorded</p>
            ) : (
              <ul className={styles.list}>
                {data.payments.map((p) => (
                  <li key={p.id} className={styles.listItem}>
                    <div>
                      <strong>{p.user}</strong>
                      <div className={styles.metaText}>
                        {p.plan} · {formatDate(p.date)}
                      </div>
                    </div>
                    <span className={styles.amount}>${p.amount.toFixed(2)}</span>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className={`${styles.card} ${styles.cardWide}`}>
            <h2 className={styles.cardTitle}>Audit Log</h2>
            {data.auditLog.length === 0 ? (
              <p className={styles.mutedText}>No activity recorded yet</p>
            ) : (
              <ul className={styles.list}>
                {data.auditLog.map((log) => (
                  <li key={log.id} className={styles.listItem}>
                    <span>
                      <strong>{log.actor}</strong> — {log.action}
                    </span>
                    <span className={styles.metaText}>{formatDate(log.time)}</span>
                  </li>
                ))}
              </ul>
            )}
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
