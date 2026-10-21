import { useCallback, useEffect, useState } from "react";
import {
  getAdminDashboard,
  listAccounts,
  createAccount,
  getAccount,
  updateAccount,
  deleteAccount,
  setAccountRole,
  setAccountStatus,
} from "../../api/adminApi";

import styles from "./AdminDashboard.module.css";

const ROLE_LABEL = {
  OWNER: "Owner",
  LEGAL_VERIFIER: "Legal Verifier",
  ADMIN: "Admin",
  USER: "User",
  BENEFICIARY: "Beneficiary",
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
  const [search, setSearch] = useState("");
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

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");

    try {
      const response = await listAccounts({
        page,
        pageSize,
        search,
        role: roleFilter,
      });

      const result = response?.data ?? response;
      const accounts = Array.isArray(result?.items)
        ? result.items
        : Array.isArray(result?.accounts)
          ? result.accounts
          : [];

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
  }, [page, pageSize, search, roleFilter]);

  useEffect(() => {
    load();
  }, [load]);

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
              <input
                className={styles.searchInput}
                type="text"
                placeholder="Search by name or email..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
              />

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
                        <AccountBadge status={acc.status} />
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


                        <button
                          type="button"
                          className={
                            acc.status === "ACTIVE"
                              ? styles.dangerBtn
                              : styles.primaryBtn
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
