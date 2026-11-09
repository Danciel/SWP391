import { useCallback, useEffect, useState } from "react";
import {
  getAdminDashboard,
  listAccounts,
  createAccount,
  getAccount,
  updateAccount,
  deleteAccount,
  setAccountRole,
} from "../../api/adminApi";

import {
  getAdminPayments,
  getAdminAuditLogs,
} from "../../api/adminOperationsApi";

import AdminPricingRules from "../../components/AdminPricingRules/AdminPricingRules";
import styles from "./AdminDashboard.module.css";

const ROLE_LABEL = {
  USER: "User (Owner / Beneficiary)",
  EXECUTOR: "Executor",
  LEGAL_VERIFIER: "Legal Verifier",
};

const ROLE_OPTIONS = Object.keys(ROLE_LABEL);

const CASE_STATUS_LABEL = {
  AWAITING_DOCUMENTS: "Awaiting Documents",
  PENDING_REVIEW: "Pending Review",
  APPROVED: "Approved",
  REJECTED: "Rejected",
};

function AccountBadge({ status }) {
  const normalizedStatus = String(status ?? "").toUpperCase();

  const isVerified = normalizedStatus === "VERIFIED";

  return (
    <span
      className={`${styles.badge} ${isVerified ? styles.ACTIVE : styles.SUSPENDED
        }`}
    >
      {isVerified ? "Verified" : "Unverified"}
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


const AUDIT_DETAIL_LABELS = {
  willId: "Will ID",
  assetId: "Asset ID",
  amount: "Amount",
  Amount: "Amount",
  paidNow: "Paid Now",
  Currency: "Currency",
  currency: "Currency",
  targetAccountId: "Target Account",
  executorAccountId: "Executor Account",
  beneficiaryId: "Beneficiary ID",
  handoverCaseId: "Handover Case",
  remainingItems: "Remaining Items",
  itemCount: "Item Count",
  type: "Handover Type",
  Title: "Will Title",
  relationship: "Relationship",
};

function parseAuditDetails(details) {
  if (!details) return {};

  if (typeof details === "object") return details;

  try {
    return JSON.parse(details);
  } catch {
    return { description: details };
  }
}

function formatAuditValue(key, value) {
  if (value === null || value === undefined || value === "") {
    return "—";
  }

  if (key.toLowerCase().includes("amount") || key === "paidNow") {
    return `${Number(value).toLocaleString("vi-VN")} VND`;
  }

  return String(value);
}


export default function AdminDashboard() {
  const [data, setData] = useState(null);
  const [auditLogs, setAuditLogs] = useState([]);
  const [auditTotal, setAuditTotal] = useState(0);
  const [auditPage, setAuditPage] = useState(1);
  const [auditPageSize] = useState(7);
  const [paymentStatus, setPaymentStatus] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);
  const [toast, setToast] = useState("");
  const [roleBusyId, setRoleBusyId] = useState(null);
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingAccount, setEditingAccount] = useState(null);
  const [isUpdating, setIsUpdating] = useState(false);
  const [createForm, setCreateForm] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    password: "",
    role: "USER",
    emailVerified: false,
    phoneVerified: false,
  });
  const [isCreating, setIsCreating] = useState(false);
  const [roleFilter, setRoleFilter] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [pagination, setPagination] = useState({
    totalItems: 0,
    totalPages: 1,
  });

  const loadPayments = useCallback(async () => {
    try {
      const result = await getAdminPayments({
        page: 1,
        pageSize: 50,
        status: paymentStatus || undefined,
      });

      setData((previous) => ({
        ...(previous ?? {}),
        payments: result?.items ?? [],
        paymentTotal: result?.total ?? 0,
      }));
    } catch (err) {
      console.error("Could not load admin payments:", err);
    }
  }, [paymentStatus]);

  const loadAuditLogs = useCallback(async () => {
    try {
      const result = await getAdminAuditLogs({
        page: auditPage,
        pageSize: auditPageSize,
      });

      setAuditLogs(result?.items ?? []);
      setAuditTotal(result?.total ?? 0);
    } catch (err) {
      console.error("Could not load audit logs:", err);
    }
  }, [auditPage, auditPageSize]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");

    try {
      const response = await listAccounts({
        page,
        pageSize,
        search: appliedSearch,
        role: roleFilter,
      });

      const result = response?.data ?? response;
      const allAccounts = Array.isArray(result?.items)
        ? result.items
        : Array.isArray(result?.accounts)
          ? result.accounts
          : [];

      const accounts = allAccounts.filter(
        (account) => account.role?.toUpperCase() !== "ADMIN"
      );
      setData((previous) => ({
        ...(previous ?? {}),
        accounts,
        cases: previous?.cases ?? [],
        payments: previous?.payments ?? [],
        auditLog: previous?.auditLog ?? [],
      }));

      setPagination({
        totalItems: result?.totalItems ?? accounts.length,
        totalPages: result?.totalPages ?? 1,
      });
    } catch (err) {
      setError(err.message || "Could not load accounts");
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, appliedSearch, roleFilter]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    loadPayments();
  }, [loadPayments]);

  useEffect(() => {
    loadAuditLogs();
  }, [loadAuditLogs]);

  function showToast(msg) {
    setToast(msg);
    setTimeout(() => setToast(""), 2500);
  }

  async function handleCreateAccount(event) {
    event.preventDefault();
    setIsCreating(true);
    setError("");

    try {
      await createAccount(createForm);

      setShowCreateForm(false);
      setCreateForm({
        fullName: "",
        email: "",
        phoneNumber: "",
        password: "",
        role: "USER",
        emailVerified: false,
        phoneVerified: false,
      });

      showToast("Account created successfully.");
      await load();
    } catch (err) {
      setError(err.message || "Could not create account.");
    } finally {
      setIsCreating(false);
    }
  }


  async function handleUpdateAccount(event) {
    event.preventDefault();

    if (!editingAccount) return;

    setIsUpdating(true);
    setError("");

    try {
      const changes = {
        fullName: editingAccount.fullName,
        email: editingAccount.email,
        phoneNumber: editingAccount.phoneNumber || null,
        role: editingAccount.role,
        emailVerified: editingAccount.emailVerified,
        phoneVerified: editingAccount.phoneVerified,
      };

      if (editingAccount.password?.trim()) {
        changes.password = editingAccount.password;
      }

      await updateAccount(editingAccount.id, changes);

      setEditingAccount(null);
      showToast("Account updated successfully.");
      await load();
    } catch (err) {
      setError(err.message || "Could not update account.");
    } finally {
      setIsUpdating(false);
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


  const paymentStats = (data?.payments ?? []).reduce(
    (stats, payment) => {
      const status = payment.status?.toUpperCase();
      const amount = Number(payment.amount) || 0;

      if (status === "HELD") {
        stats.held += amount;
        stats.heldCount++;
      }

      if (status === "RELEASED") {
        stats.released += amount;
        stats.releasedCount++;
      }

      if (status === "REFUNDED") {
        stats.refunded += amount;
        stats.refundedCount++;
      }

      return stats;
    },
    {
      held: 0,
      released: 0,
      refunded: 0,
      heldCount: 0,
      releasedCount: 0,
      refundedCount: 0,
    }
  );

  const formatMoney = (amount) =>
    new Intl.NumberFormat("vi-VN").format(amount) + " VND";

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
            <div className={styles.accountsHeader}>
              <h2 className={styles.cardTitle}>
                Accounts ({pagination.totalItems})
              </h2>

              <button
                type="button"
                className={styles.primaryBtn}
                onClick={() => setShowCreateForm(current => !current)}
              >
                + Add Account
              </button>
            </div>


            {showCreateForm && (
              <form
                className={styles.createForm}
                onSubmit={handleCreateAccount}
              >
                <h3 className={styles.cardTitle}>Create Account</h3>

                <label className={styles.formField}>
                  Full Name *
                  <input
                    className={styles.searchInput}
                    type="text"
                    value={createForm.fullName}
                    onChange={e =>
                      setCreateForm({ ...createForm, fullName: e.target.value })
                    }
                    required
                  />
                </label>

                <label className={styles.formField}>
                  Email *
                  <input
                    className={styles.searchInput}
                    type="email"
                    value={createForm.email}
                    onChange={e =>
                      setCreateForm({ ...createForm, email: e.target.value })
                    }
                    required
                  />
                </label>

                <label className={styles.formField}>
                  Phone Number
                  <input
                    className={styles.searchInput}
                    type="tel"
                    value={createForm.phoneNumber}
                    onChange={e =>
                      setCreateForm({ ...createForm, phoneNumber: e.target.value })
                    }
                  />
                </label>

                <label className={styles.formField}>
                  Password *
                  <input
                    className={styles.searchInput}
                    type="password"
                    value={createForm.password}
                    onChange={e =>
                      setCreateForm({ ...createForm, password: e.target.value })
                    }
                    minLength={8}
                    required
                  />
                </label>

                <label className={styles.formField}>
                  Role *
                  <select
                    className={styles.roleSelect}
                    value={createForm.role}
                    onChange={e =>
                      setCreateForm({ ...createForm, role: e.target.value })
                    }
                    required
                  >
                    {ROLE_OPTIONS.map(role => (
                      <option key={role} value={role}>
                        {ROLE_LABEL[role]}
                      </option>
                    ))}
                  </select>
                </label>

                <label className={styles.formCheckbox}>
                  <input
                    type="checkbox"
                    checked={createForm.emailVerified}
                    onChange={e =>
                      setCreateForm({
                        ...createForm,
                        emailVerified: e.target.checked,
                      })
                    }
                  />
                  Email verified
                </label>

                <label className={styles.formCheckbox}>
                  <input
                    type="checkbox"
                    checked={createForm.phoneVerified}
                    onChange={e =>
                      setCreateForm({
                        ...createForm,
                        phoneVerified: e.target.checked,
                      })
                    }
                  />
                  Phone verified
                </label>

                <div className={styles.formActions}>
                  <button
                    type="submit"
                    className={styles.primaryBtn}
                    disabled={isCreating}
                  >
                    {isCreating ? "Creating..." : "Create Account"}
                  </button>

                  <button
                    type="button"
                    className={styles.dangerBtn}
                    disabled={isCreating}
                    onClick={() => setShowCreateForm(false)}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}


            {editingAccount && (
              <form
                className={styles.createForm}
                onSubmit={handleUpdateAccount}
              >
                <h3 className={styles.cardTitle}>Edit Account</h3>

                <label className={styles.formField}>
                  Full Name *
                  <input
                    className={styles.searchInput}
                    type="text"
                    value={editingAccount.fullName}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        fullName: e.target.value,
                      })
                    }
                    required
                  />
                </label>

                <label className={styles.formField}>
                  Email *
                  <input
                    className={styles.searchInput}
                    type="email"
                    value={editingAccount.email}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        email: e.target.value,
                      })
                    }
                    required
                  />
                </label>

                <label className={styles.formField}>
                  Phone Number
                  <input
                    className={styles.searchInput}
                    type="tel"
                    value={editingAccount.phoneNumber}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        phoneNumber: e.target.value,
                      })
                    }
                  />
                </label>

                <label className={styles.formField}>
                  New Password
                  <input
                    className={styles.searchInput}
                    type="password"
                    value={editingAccount.password}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        password: e.target.value,
                      })
                    }
                    minLength={8}
                    placeholder="Leave blank to keep current password"
                  />
                </label>

                <label className={styles.formField}>
                  Role *
                  <select
                    className={styles.roleSelect}
                    value={editingAccount.role}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        role: e.target.value,
                      })
                    }
                    required
                  >
                    {ROLE_OPTIONS.map(role => (
                      <option key={role} value={role}>
                        {ROLE_LABEL[role]}
                      </option>
                    ))}
                  </select>
                </label>

                <label className={styles.formCheckbox}>
                  <input
                    type="checkbox"
                    checked={editingAccount.emailVerified}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        emailVerified: e.target.checked,
                      })
                    }
                  />
                  Email verified
                </label>

                <label className={styles.formCheckbox}>
                  <input
                    type="checkbox"
                    checked={editingAccount.phoneVerified}
                    onChange={e =>
                      setEditingAccount({
                        ...editingAccount,
                        phoneVerified: e.target.checked,
                      })
                    }
                  />
                  Phone verified
                </label>

                <div className={styles.formActions}>
                  <button
                    type="submit"
                    className={styles.primaryBtn}
                    disabled={isUpdating}
                  >
                    {isUpdating ? "Saving..." : "Save Changes"}
                  </button>

                  <button
                    type="button"
                    className={styles.dangerBtn}
                    disabled={isUpdating}
                    onClick={() => setEditingAccount(null)}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}





            <div className={styles.accountToolbar}>
              <div className={styles.searchGroup}>
                <input
                  className={styles.searchInput}
                  type="text"
                  placeholder="Search by name or email..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      setPage(1);
                      setAppliedSearch(search.trim());
                    }
                  }}
                />

                <button
                  type="button"
                  className={styles.primaryBtn}
                  onClick={() => {
                    setPage(1);
                    setAppliedSearch(search.trim());
                  }}
                >
                  Search
                </button>
              </div>

              <select
                className={styles.roleSelect}
                value={roleFilter}
                onChange={(e) => {
                  setRoleFilter(e.target.value);
                  setPage(1);
                }}
              >
                <option value="">All roles</option>
                {ROLE_OPTIONS.map((role) => (
                  <option key={role} value={role}>
                    {ROLE_LABEL[role]}
                  </option>
                ))}
              </select>
            </div>


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
                      <td>{acc.fullName || acc.name || "—"}</td>
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
                        <AccountBadge status={acc.identityStatus} />
                      </td>

                      <td className={styles.actionCell}>
                        <button
                          type="button"
                          className={styles.primaryBtn}
                          onClick={() =>
                            setEditingAccount({
                              id: acc.id,
                              fullName: acc.fullName || acc.name || "",
                              email: acc.email || "",
                              phoneNumber: acc.phoneNumber || "",
                              password: "",
                              role: acc.role || "USER",
                              emailVerified: acc.emailVerified ?? false,
                              phoneVerified: acc.phoneVerified ?? false,
                            })
                          }
                        >
                          Edit
                        </button>


                        <button
                          type="button"
                          className={styles.dangerBtn}
                          disabled={busyId === acc.id}
                          onClick={async () => {
                            const confirmed = window.confirm(
                              `Are you sure you want to delete ${acc.fullName || acc.email}?`
                            );

                            if (!confirmed) return;

                            setBusyId(acc.id);
                            setError("");

                            try {
                              await deleteAccount(acc.id);
                              showToast("Account deleted successfully.");
                              await load();
                            } catch (err) {
                              setError(err.message || "Could not delete account.");
                            } finally {
                              setBusyId(null);
                            }
                          }}
                        >
                          Delete
                        </button>


                      </td>

                    </tr>
                  ))}

                </tbody>
              </table>
            </div>

            {/* Account pagination */}
            <div className={styles.pagination}>
              <span className={styles.mutedText}>
                Page {page} of {pagination.totalPages}
                {" · "}
                {pagination.totalItems} accounts
              </span>

              <div className={styles.paginationActions}>
                <button
                  type="button"
                  className={styles.primaryBtn}
                  disabled={page <= 1 || isLoading}
                  onClick={() => setPage(current => current - 1)}
                >
                  Previous
                </button>

                <button
                  type="button"
                  className={styles.primaryBtn}
                  disabled={page >= pagination.totalPages || isLoading}
                  onClick={() => setPage(current => current + 1)}
                >
                  Next
                </button>
              </div>
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

          <section className={`${styles.card} ${styles.cardWide}`}>
            <h2 className={styles.cardTitle}>
              Payments ({data?.paymentTotal ?? data?.payments?.length ?? 0})
            </h2>

            <div className={styles.paymentStats}>

              {/* HELD */}
              <div className={`${styles.paymentStatCard} ${styles.heldCard}`}>
                <div className={styles.paymentStatHeader}>
                  <span>Held Balance</span>
                  <span className={styles.heldBadge}>HELD</span>
                </div>

                <h3>{formatMoney(paymentStats.held)}</h3>

                <p>
                  {paymentStats.heldCount} {paymentStats.heldCount === 1 ? "transaction" : "transactions"}
                </p>
              </div>

              {/* RELEASED */}
              <div className={`${styles.paymentStatCard} ${styles.releasedCard}`}>
                <div className={styles.paymentStatHeader}>
                  <span>Released</span>
                  <span className={styles.releasedBadge}>RELEASED</span>
                </div>

                <h3>{formatMoney(paymentStats.released)}</h3>

                <p>
                  {paymentStats.releasedCount} {paymentStats.releasedCount === 1 ? "transaction" : "transactions"}
                </p>
              </div>

              {/* REFUNDED */}
              <div className={`${styles.paymentStatCard} ${styles.refundedCard}`}>
                <div className={styles.paymentStatHeader}>
                  <span>Refunded</span>
                  <span className={styles.refundedBadge}>REFUNDED</span>
                </div>

                <h3>{formatMoney(paymentStats.refunded)}</h3>

                <p>
                  {paymentStats.refundedCount} {paymentStats.refundedCount === 1 ? "transaction" : "transactions"}
                </p>
              </div>

            </div>

            <div style={{ marginBottom: 16 }}>
              <select
                value={paymentStatus}
                onChange={(e) => setPaymentStatus(e.target.value)}
              >
                <option value="">All statuses</option>
                <option value="HELD">Held</option>
                <option value="RELEASED">Released</option>
                <option value="REFUNDED">Refunded</option>
              </select>
            </div>

            {!data?.payments?.length ? (
              <p className={styles.mutedText}>
                No payments recorded
              </p>
            ) : (
              <div style={{ overflowX: "auto" }}>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>Will</th>
                      <th>Owner</th>
                      <th>Executor</th>
                      <th>Amount</th>
                      <th>Status</th>
                      <th>Held At</th>
                      <th>Released / Refunded</th>
                      <th>Ledger Details</th>
                    </tr>
                  </thead>

                  <tbody>
                    {data.payments.map((payment) => (
                      <tr key={payment.id}>
                        <td>{payment.willTitle || payment.willId}</td>

                        <td>{payment.ownerName || "N/A"}</td>

                        <td>{payment.executorName || "N/A"}</td>

                        <td>
                          {new Intl.NumberFormat("vi-VN", {
                            style: "currency",
                            currency: payment.currency || "VND",
                          }).format(payment.amount || 0)}
                        </td>

                        <td>
                          <CaseBadge status={payment.status} />
                        </td>

                        <td>
                          {formatDate(payment.heldAt)}
                        </td>

                        <td>
                          {formatDate(
                            payment.releasedAt || payment.refundedAt
                          )}
                        </td>
                        <td>
                          <details>
                            <summary style={{ cursor: "pointer", color: "#176451" }}>
                              View Ledger ({payment.ledger?.length ?? 0})
                            </summary>

                            {!payment.ledger?.length ? (
                              <p>No ledger transactions</p>
                            ) : (
                              <div style={{ minWidth: 300, marginTop: 10 }}>
                                {payment.ledger.map((item) => (
                                  <div
                                    key={item.id}
                                    style={{
                                      padding: "10px",
                                      borderBottom: "1px solid #ddd",
                                    }}
                                  >
                                    <strong>{item.type}</strong>

                                    <div>
                                      Amount: {Number(item.amount || 0).toLocaleString("vi-VN")} VND
                                    </div>

                                    <div>
                                      Balance after: {Number(item.balanceAfter || 0).toLocaleString("vi-VN")} VND
                                    </div>

                                    <div>
                                      Note: {item.note || "—"}
                                    </div>

                                    <small>
                                      {formatDate(item.createdAt)}
                                    </small>
                                  </div>
                                ))}
                              </div>
                            )}
                          </details>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <AdminPricingRules />

          <section className={`${styles.card} ${styles.cardWide}`}>

            <h2 className={styles.cardTitle}>
              Audit Log ({auditTotal})
            </h2>

            {auditLogs.length === 0 ? (
              <p className={styles.mutedText}>
                No activity recorded yet
              </p>
            ) : (
              <div style={{ overflowX: "auto" }}>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>Actor</th>
                      <th>Role</th>
                      <th>Action</th>
                      <th>Entity</th>
                      <th>Details</th>
                      <th>Time</th>
                    </tr>
                  </thead>

                  <tbody>
                    {auditLogs.map((log) => (
                      <tr key={log.id}>
                        <td>{log.actorName || "System"}</td>

                        <td>{log.actorRole || "N/A"}</td>

                        <td>
                          <strong>{log.action || "N/A"}</strong>
                        </td>


                        <td>
                          <div className={styles.auditEntity}>
                            <strong>
                              {log.entityType || "Unknown"}
                            </strong>

                            <div
                              className={styles.auditEntityName}
                              title={log.entityName || log.entityId}
                            >
                              {log.entityName || "No entity name"}
                            </div>
                          </div>
                        </td>

                        <td>
                          {(() => {
                            const details = parseAuditDetails(log.details);
                            const entries = Object.entries(details);

                            if (!entries.length) return "—";

                            return (
                              <details className={styles.auditDetails}>
                                <summary className={styles.auditDetailsSummary}>
                                  View Details ({entries.length})
                                </summary>

                                <div className={styles.auditDetailsContent}>
                                  {entries.map(([key, value]) => (
                                    <div
                                      key={key}
                                      className={styles.auditDetailRow}
                                    >
                                      <strong className={styles.auditDetailLabel}>
                                        {AUDIT_DETAIL_LABELS[key] || key}
                                      </strong>

                                      <span className={styles.auditDetailValue}>
                                        {formatAuditValue(key, value)}
                                      </span>
                                    </div>
                                  ))}
                                </div>
                              </details>
                            );
                          })()}
                        </td>


                        <td>
                          {formatDate(log.createdAt)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <div className={styles.auditPagination}>
              <span className={styles.auditPaginationInfo}>
                Page {auditPage} of{" "}
                {Math.max(1, Math.ceil(auditTotal / auditPageSize))}
                {" · "}
                {auditTotal} activities
              </span>

              <div className={styles.auditPaginationActions}>
                <button
                  type="button"
                  className={styles.auditPageBtn}
                  disabled={auditPage <= 1}
                  onClick={() => setAuditPage((page) => page - 1)}
                >
                  Previous
                </button>

                <button
                  type="button"
                  className={styles.auditPageBtn}
                  disabled={
                    auditPage >= Math.ceil(auditTotal / auditPageSize)
                  }
                  onClick={() => setAuditPage((page) => page + 1)}
                >
                  Next
                </button>
              </div>
            </div>

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
