import { useCallback, useEffect, useState } from "react";
import {
    listIdpUsers,
    createIdpUser,
    updateIdpUser,
    deleteIdpUser,
    enrollIdpFace,
} from "../../api/idpUserApi";
import { deleteFace } from "../../api/faceRecognitionApi";
import styles from "./IdpAdminDashboard.module.css";

const IDENTITY_STATUS_OPTIONS = ["UNVERIFIED", "VERIFIED", "SUSPENDED"];
const LIFE_STATUS_OPTIONS = ["ALIVE", "DECEASED"];

export default function IdpAdminDashboard() {
    const [users, setUsers] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState("");
    const [toast, setToast] = useState("");
    const [busyId, setBusyId] = useState(null);

    // Filters & Pagination
    const [search, setSearch] = useState("");
    const [appliedSearch, setAppliedSearch] = useState("");
    const [statusFilter, setStatusFilter] = useState("");
    const [page, setPage] = useState(1);
    const [pageSize] = useState(20);
    const [pagination, setPagination] = useState({ totalItems: 0, totalPages: 1 });

    // Modals / Forms
    const [showCreateForm, setShowCreateForm] = useState(false);
    const [editingUser, setEditingUser] = useState(null);
    const [enrollingUser, setEnrollingUser] = useState(null);
    const [faceFile, setFaceFile] = useState(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Form States
    const [createForm, setCreateForm] = useState({
        fullName: "",
        email: "",
        phoneNumber: "",
        identityStatus: "UNVERIFIED",
        lifeStatus: "ALIVE",
    });

    const load = useCallback(async () => {
        setIsLoading(true);
        setError("");

        try {
            const response = await listIdpUsers({
                page,
                pageSize,
                search: appliedSearch,
                identityStatus: statusFilter,
            });

            setUsers(response?.items ?? []);
            setPagination({
                totalItems: response?.totalItems ?? 0,
                totalPages: response?.totalPages ?? 1,
            });
        } catch (err) {
            setError(err.message || "Could not load IDP users.");
        } finally {
            setIsLoading(false);
        }
    }, [page, pageSize, appliedSearch, statusFilter]);

    useEffect(() => {
        load();
    }, [load]);

    function showToast(msg) {
        setToast(msg);
        setTimeout(() => setToast(""), 3000);
    }

    // --- CRUD Handlers ---

    async function handleCreate(event) {
        event.preventDefault();
        setIsSubmitting(true);
        setError("");

        try {
            await createIdpUser(createForm);
            setShowCreateForm(false);
            setCreateForm({ fullName: "", email: "", phoneNumber: "", identityStatus: "UNVERIFIED", lifeStatus: "ALIVE" });
            showToast("IDP User created successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Could not create IDP user.");
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleUpdate(event) {
        event.preventDefault();
        if (!editingUser) return;
        setIsSubmitting(true);
        setError("");

        try {
            await updateIdpUser(editingUser.id, {
                fullName: editingUser.fullName,
                email: editingUser.email,
                phoneNumber: editingUser.phoneNumber,
                identityStatus: editingUser.identityStatus,
                lifeStatus: editingUser.lifeStatus,
            });
            setEditingUser(null);
            showToast("IDP User updated successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Could not update IDP user.");
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleDelete(id) {
        if (!window.confirm("Are you sure you want to delete this IDP user?")) return;
        setBusyId(id);
        setError("");

        try {
            await deleteIdpUser(id);
            showToast("IDP User deleted successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Could not delete IDP user.");
        } finally {
            setBusyId(null);
        }
    }

    // --- Face Enrollment Handlers ---

    async function handleEnrollFace(event) {
        event.preventDefault();
        if (!faceFile || !enrollingUser) return;
        setIsSubmitting(true);
        setError("");

        try {
            await enrollIdpFace(enrollingUser.id, faceFile);
            setEnrollingUser(null);
            setFaceFile(null);
            showToast("Face enrolled successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Could not enroll face.");
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleDeleteFace(id) {
        if (!window.confirm("Are you sure you want to remove this user's face enrollment?")) return;
        setBusyId(id);
        setError("");

        try {
            await deleteFace(id);
            showToast("Face enrollment removed.");
            await load();
        } catch (err) {
            setError(err.message || "Could not delete face enrollment.");
        } finally {
            setBusyId(null);
        }
    }

    return (
        <div className={styles.page}>
            <header className={styles.header}>
                <h1 className={styles.title}>IDP Admin Dashboard</h1>
                <p className={styles.subtitle}>
                    Manage identity profiles, statuses, and facial recognition enrollments.
                </p>
            </header>

            {toast && <div className={styles.toast}>{toast}</div>}
            {error && <p className={styles.errorBox}>{error}</p>}

            <section className={`${styles.card} ${styles.cardWide}`}>
                <div className={styles.accountsHeader}>
                    <h2 className={styles.cardTitle}>IDP Users ({pagination.totalItems})</h2>
                    <button
                        type="button"
                        className={styles.primaryBtn}
                        onClick={() => setShowCreateForm((prev) => !prev)}
                    >
                        + Add IDP User
                    </button>
                </div>

                {/* --- CREATE FORM --- */}
                {showCreateForm && (
                    <form className={styles.createForm} onSubmit={handleCreate}>
                        <h3 className={styles.cardTitle}>Create IDP User</h3>

                        <label className={styles.formField}>
                            Full Name *
                            <input
                                className={styles.searchInput}
                                type="text"
                                required
                                value={createForm.fullName}
                                onChange={(e) => setCreateForm({ ...createForm, fullName: e.target.value })}
                            />
                        </label>

                        <label className={styles.formField}>
                            Email *
                            <input
                                className={styles.searchInput}
                                type="email"
                                required
                                value={createForm.email}
                                onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                            />
                        </label>

                        <label className={styles.formField}>
                            Phone Number *
                            <input
                                className={styles.searchInput}
                                type="tel"
                                required
                                value={createForm.phoneNumber}
                                onChange={(e) => setCreateForm({ ...createForm, phoneNumber: e.target.value })}
                            />
                        </label>

                        <label className={styles.formField}>
                            Identity Status
                            <select
                                className={styles.roleSelect}
                                value={createForm.identityStatus}
                                onChange={(e) => setCreateForm({ ...createForm, identityStatus: e.target.value })}
                            >
                                {IDENTITY_STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                            </select>
                        </label>

                        <label className={styles.formField}>
                            Life Status
                            <select
                                className={styles.roleSelect}
                                value={createForm.lifeStatus}
                                onChange={(e) => setCreateForm({ ...createForm, lifeStatus: e.target.value })}
                            >
                                {LIFE_STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                            </select>
                        </label>

                        <div className={styles.formActions}>
                            <button type="submit" className={styles.primaryBtn} disabled={isSubmitting}>
                                {isSubmitting ? "Creating..." : "Create User"}
                            </button>
                            <button type="button" className={styles.dangerBtn} onClick={() => setShowCreateForm(false)}>
                                Cancel
                            </button>
                        </div>
                    </form>
                )}

                {/* --- EDIT FORM --- */}
                {editingUser && (
                    <form className={styles.createForm} onSubmit={handleUpdate}>
                        <h3 className={styles.cardTitle}>Edit IDP User</h3>

                        <label className={styles.formField}>
                            Full Name *
                            <input
                                className={styles.searchInput}
                                type="text"
                                required
                                value={editingUser.fullName}
                                onChange={(e) => setEditingUser({ ...editingUser, fullName: e.target.value })}
                            />
                        </label>

                        <label className={styles.formField}>
                            Email *
                            <input
                                className={styles.searchInput}
                                type="email"
                                required
                                value={editingUser.email}
                                onChange={(e) => setEditingUser({ ...editingUser, email: e.target.value })}
                            />
                        </label>

                        <label className={styles.formField}>
                            Phone Number *
                            <input
                                className={styles.searchInput}
                                type="tel"
                                required
                                value={editingUser.phoneNumber || ""}
                                onChange={(e) => setEditingUser({ ...editingUser, phoneNumber: e.target.value })}
                            />
                        </label>

                        <label className={styles.formField}>
                            Identity Status
                            <select
                                className={styles.roleSelect}
                                value={editingUser.identityStatus}
                                onChange={(e) => setEditingUser({ ...editingUser, identityStatus: e.target.value })}
                            >
                                {IDENTITY_STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                            </select>
                        </label>

                        <label className={styles.formField}>
                            Life Status
                            <select
                                className={styles.roleSelect}
                                value={editingUser.lifeStatus}
                                onChange={(e) => setEditingUser({ ...editingUser, lifeStatus: e.target.value })}
                            >
                                {LIFE_STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                            </select>
                        </label>

                        <div className={styles.formActions}>
                            <button type="submit" className={styles.primaryBtn} disabled={isSubmitting}>
                                {isSubmitting ? "Saving..." : "Save Changes"}
                            </button>
                            <button type="button" className={styles.dangerBtn} onClick={() => setEditingUser(null)}>
                                Cancel
                            </button>
                        </div>
                    </form>
                )}

                {/* --- FACE ENROLLMENT FORM --- */}
                {enrollingUser && (
                    <form className={styles.createForm} onSubmit={handleEnrollFace}>
                        <h3 className={styles.cardTitle}>Enroll Face for {enrollingUser.fullName}</h3>

                        <label className={styles.formField} style={{ gridColumn: "1 / -1" }}>
                            Upload Face Image (JPEG/PNG)
                            <input
                                className={styles.searchInput}
                                type="file"
                                accept="image/jpeg, image/png"
                                required
                                onChange={(e) => setFaceFile(e.target.files[0])}
                            />
                        </label>

                        <div className={styles.formActions}>
                            <button type="submit" className={styles.primaryBtn} disabled={isSubmitting || !faceFile}>
                                {isSubmitting ? "Uploading..." : "Enroll Face"}
                            </button>
                            <button type="button" className={styles.dangerBtn} onClick={() => { setEnrollingUser(null); setFaceFile(null); }}>
                                Cancel
                            </button>
                        </div>
                    </form>
                )}

                {/* --- TOOLBAR --- */}
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
                        value={statusFilter}
                        onChange={(e) => {
                            setStatusFilter(e.target.value);
                            setPage(1);
                        }}
                    >
                        <option value="">All Identity Statuses</option>
                        {IDENTITY_STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                    </select>
                </div>

                {/* --- TABLE --- */}
                <div className={styles.tableWrap}>
                    <table className={styles.table}>
                        <thead>
                        <tr>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Identity Status</th>
                            <th>Life Status</th>
                            <th>Face Enrolled</th>
                            <th>Actions</th>
                        </tr>
                        </thead>
                        <tbody>
                        {isLoading ? (
                            <tr><td colSpan="6" style={{ textAlign: "center", padding: "20px" }}>Loading...</td></tr>
                        ) : users.length === 0 ? (
                            <tr><td colSpan="6" style={{ textAlign: "center", padding: "20px" }}>No users found.</td></tr>
                        ) : (
                            users.map((user) => (
                                <tr key={user.id}>
                                    <td>{user.fullName}</td>
                                    <td>{user.email}</td>
                                    <td>
                      <span className={`${styles.badge} ${styles[user.identityStatus] || ""}`}>
                        {user.identityStatus}
                      </span>
                                    </td>
                                    <td>
                      <span className={`${styles.badge} ${user.lifeStatus === "ALIVE" ? styles.ACTIVE : styles.SUSPENDED}`}>
                        {user.lifeStatus}
                      </span>
                                    </td>
                                    <td>
                                        {user.faceId ? (
                                            <span className={styles.faceEnrolled}>✓ Enrolled</span>
                                        ) : (
                                            <span className={styles.faceMissing}>— Not Enrolled</span>
                                        )}
                                    </td>
                                    <td className={styles.actionCell}>
                                        <button
                                            type="button"
                                            className={styles.primaryBtn}
                                            onClick={() => setEditingUser(user)}
                                        >
                                            Edit
                                        </button>

                                        {user.faceId ? (
                                            <button
                                                type="button"
                                                className={styles.dangerBtn}
                                                disabled={busyId === user.id}
                                                onClick={() => handleDeleteFace(user.id)}
                                            >
                                                Remove Face
                                            </button>
                                        ) : (
                                            <button
                                                type="button"
                                                className={styles.primaryBtn}
                                                onClick={() => setEnrollingUser(user)}
                                            >
                                                Enroll Face
                                            </button>
                                        )}

                                        <button
                                            type="button"
                                            className={styles.dangerBtn}
                                            disabled={busyId === user.id}
                                            onClick={() => handleDelete(user.id)}
                                        >
                                            Delete
                                        </button>
                                    </td>
                                </tr>
                            ))
                        )}
                        </tbody>
                    </table>
                </div>

                {/* --- PAGINATION --- */}
                <div className={styles.pagination}>
                <span className={styles.mutedText}>
                    Page {page} of {pagination.totalPages} · {pagination.totalItems} users
                </span>
                    <div className={styles.paginationActions}>
                        <button
                            type="button"
                            className={styles.primaryBtn}
                            disabled={page <= 1 || isLoading}
                            onClick={() => setPage((prev) => prev - 1)}
                        >
                            Previous
                        </button>
                        <button
                            type="button"
                            className={styles.primaryBtn}
                            disabled={page >= pagination.totalPages || isLoading}
                            onClick={() => setPage((prev) => prev + 1)}
                        >
                            Next
                        </button>
                    </div>
                </div>
            </section>
        </div>
    );
}