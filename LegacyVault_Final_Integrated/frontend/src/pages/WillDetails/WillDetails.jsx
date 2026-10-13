import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import {
  getWill,
  getAssets,
  updateWillOverview,
  updateWillBeneficiary,
  changeWillExecutor,
  updateWillDms,
  checkInDms,
  cancelDeathHandover,
  requestImmediateHandover,
  EXECUTORS,
} from "../../api/mockVault";
import styles from "./WillDetails.module.css";

const TABS = ["Overview", "Assets", "Beneficiaries", "Executor", "DMS"];

export default function WillDetails() {
  const { id } = useParams();
  const { user } = useAuth();
  const [will, setWill] = useState(null);
  const [assets, setAssets] = useState([]);
  const [tab, setTab] = useState("Overview");
  const [loading, setLoading] = useState(true);
  const [confirmingHandover, setConfirmingHandover] = useState(false);
  const [handoverBusy, setHandoverBusy] = useState(false);
  const [handoverError, setHandoverError] = useState("");

  useEffect(() => {
    load();
  }, [id]);

  async function load() {
    setLoading(true);
    const [willData, assetsData] = await Promise.all([getWill(user?.id, id), getAssets(user?.id)]);
    setWill(willData);
    setAssets(assetsData);
    setLoading(false);
  }

  if (loading || !will) return <p className={styles.muted}>Loading...</p>;

  function assetName(assetId) {
    return assets.find((a) => a.id === assetId)?.name || "Asset";
  }

  async function handleConfirmImmediateHandover() {
    setHandoverBusy(true);
    setHandoverError("");
    try {
      const updated = await requestImmediateHandover(user?.id, will.id);
      setWill(updated);
      setConfirmingHandover(false);
    } catch (err) {
      setHandoverError(err.message || "Could not request immediate handover");
    } finally {
      setHandoverBusy(false);
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.headRow}>
        <h1 className={styles.title}>{will.name}</h1>
        <span className={`${styles.badge} ${styles[will.status] || ""}`}>{will.status.replaceAll("_", " ")}</span>
      </div>
      <Link to="/dashboard/my-wills" className={styles.backLink}>
        ← Back to My Wills
      </Link>

      {will.status === "PENDING_HANDOVER_REVIEW" && (
        <div className={styles.pendingNote}>
          Immediate handover request sent to {will.executor?.name || "your executor"}. Waiting for them
          to confirm the beneficiary(ies) and respond
          {will.handover?.deadlineAt ? ` by ${new Date(will.handover.deadlineAt).toLocaleString("en-US")}` : ""}.
        </div>
      )}

      {will.status === "HANDED_OVER" && (
        <div className={styles.pendingNote}>
          This will's assets have been handed over to your executor and delivered to the assigned
          beneficiary(ies).
        </div>
      )}

      {will.status === "ACTIVE" && will.handover?.decision === "DECLINED" && (
        <div className={styles.declineNote}>
          Your executor declined the immediate handover request
          {will.handover?.respondedAt ? ` on ${new Date(will.handover.respondedAt).toLocaleDateString()}` : ""}.
          You can review the beneficiary details and try again below.
        </div>
      )}

      {will.status === "ACTIVE" && (
        <div className={styles.demoRow}>
          <button className={styles.secondaryBtn} onClick={() => setConfirmingHandover(true)}>
            Immediate Handover
          </button>
        </div>
      )}

      {confirmingHandover && (
        <div className={styles.overlay} onClick={() => !handoverBusy && setConfirmingHandover(false)}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Are you sure?</h2>
            <p className={styles.itemMeta}>
              This will send "{will.name}" to {will.executor?.name || "your executor"} to hand its assets
              over now, while you're still alive. Your executor will confirm the beneficiary details before
              anything is delivered.
            </p>
            {handoverError && <p className={styles.errorText}>{handoverError}</p>}
            <div className={styles.actions}>
              <button
                className={styles.secondaryBtn}
                disabled={handoverBusy}
                onClick={() => setConfirmingHandover(false)}
              >
                Cancel
              </button>
              <button className={styles.primaryBtn} disabled={handoverBusy} onClick={handleConfirmImmediateHandover}>
                {handoverBusy ? "Sending..." : "Yes, Send to Executor"}
              </button>
            </div>
          </div>
        </div>
      )}

      {will.status === "PENDING_EXECUTOR_ACCEPTANCE" && (
        <div className={styles.pendingNote}>
          Waiting for {will.executor?.name} to accept the executor request. Payment is currently HELD.
          {will.hireRequest?.deadlineAt
            ? ` They have until ${new Date(will.hireRequest.deadlineAt).toLocaleString("en-US")} to respond.`
            : ""}
        </div>
      )}

      <div className={styles.tabRow}>
        {TABS.map((t) => (
          <button key={t} className={tab === t ? styles.tabActive : styles.tab} onClick={() => setTab(t)}>
            {t}
          </button>
        ))}
      </div>

      <div className={styles.card}>
        {tab === "Overview" && <OverviewTab will={will} userId={user?.id} onUpdated={setWill} />}
        {tab === "Assets" && <AssetsTab will={will} assetName={assetName} />}
        {tab === "Beneficiaries" && (
          <BeneficiariesTab will={will} assets={assets} userId={user?.id} onUpdated={setWill} />
        )}
        {tab === "Executor" && <ExecutorTab will={will} userId={user?.id} onUpdated={setWill} />}
        {tab === "DMS" && <DmsTab will={will} userId={user?.id} onUpdated={setWill} />}
      </div>
    </div>
  );
}

function OverviewTab({ will, userId, onUpdated }) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(will.name);
  const [description, setDescription] = useState(will.description);
  const [saving, setSaving] = useState(false);

  async function handleSave() {
    setSaving(true);
    const updated = await updateWillOverview(userId, will.id, { name, description });
    onUpdated(updated);
    setSaving(false);
    setEditing(false);
  }

  return (
    <div>
      {editing ? (
        <>
          <label className={styles.label}>Will Name</label>
          <input className={styles.input} value={name} onChange={(e) => setName(e.target.value)} />
          <label className={styles.label}>Description</label>
          <textarea className={styles.textarea} value={description} onChange={(e) => setDescription(e.target.value)} />
          <div className={styles.actions}>
            <button className={styles.secondaryBtn} onClick={() => setEditing(false)}>
              Cancel
            </button>
            <button className={styles.primaryBtn} disabled={saving} onClick={handleSave}>
              Save Changes
            </button>
          </div>
        </>
      ) : (
        <>
          <Row label="Will Name" value={will.name} onEdit={() => setEditing(true)} />
          <Row label="Description" value={will.description || "—"} />
          <Row label="Status" value={will.status.replaceAll("_", " ")} />
          <Row label="Created Date" value={new Date(will.createdAt).toLocaleDateString()} />
          <Row label="Assets" value={(will.assets || []).length} />
          <Row label="Beneficiaries" value={(will.assets || []).filter((a) => a.beneficiary).length} />
          {will.executor && <Row label="Executor" value={will.executor.name} />}
          {will.payment?.status !== "NONE" && (
            <Row label="Payment" value={`${will.payment.fee?.toLocaleString("vi-VN")} VND · ${will.payment.status}`} />
          )}
          {will.dms?.enabled && <Row label="Dead Man's Switch" value={will.dms.status} />}
        </>
      )}
    </div>
  );
}

function Row({ label, value, onEdit }) {
  return (
    <div className={styles.row}>
      <span className={styles.rowLabel}>{label}</span>
      <span className={styles.rowValue}>
        {value} {onEdit && <button className={styles.linkBtn} onClick={onEdit}>Edit</button>}
      </span>
    </div>
  );
}

function AssetsTab({ will, assetName }) {
  return (
    <div>
      {(will.assets || []).map((a) => (
        <div key={a.assetId} className={styles.assetRow}>
          <div>
            <p className={styles.itemTitle}>{assetName(a.assetId)}</p>
            <p className={styles.itemMeta}>Status: Secured 🔒</p>
          </div>
          <div>
            <p className={styles.itemMeta}>Assigned To</p>
            <p className={styles.itemTitle}>{a.beneficiary?.fullName || "—"}</p>
          </div>
        </div>
      ))}
      <p className={styles.hintSmall}>Assets are locked to this will once submitted; add new assets in a new will.</p>
    </div>
  );
}

function BeneficiariesTab({ will, assets, userId, onUpdated }) {
  const [editingAssetId, setEditingAssetId] = useState(null);
  const [form, setForm] = useState({ fullName: "", email: "", phone: "", relationship: "Daughter" });
  const [saving, setSaving] = useState(false);

  function assetName(assetId) {
    return assets.find((a) => a.id === assetId)?.name || "Asset";
  }

  function startEdit(entry) {
    setEditingAssetId(entry.assetId);
    setForm(entry.beneficiary || { fullName: "", email: "", phone: "", relationship: "Daughter" });
  }

  async function handleSave() {
    setSaving(true);
    const updated = await updateWillBeneficiary(userId, will.id, editingAssetId, form);
    onUpdated(updated);
    setSaving(false);
    setEditingAssetId(null);
  }

  return (
    <div>
      {(will.assets || []).map((entry) => (
        <div key={entry.assetId} className={styles.assetRow}>
          {editingAssetId === entry.assetId ? (
            <div className={styles.editForm}>
              <label className={styles.label}>Full Name</label>
              <input className={styles.input} value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
              <label className={styles.label}>Email</label>
              <input className={styles.input} value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
              <label className={styles.label}>Phone</label>
              <input className={styles.input} value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              <label className={styles.label}>Relationship</label>
              <input className={styles.input} value={form.relationship} onChange={(e) => setForm({ ...form, relationship: e.target.value })} />
              <div className={styles.actions}>
                <button className={styles.secondaryBtn} onClick={() => setEditingAssetId(null)}>
                  Cancel
                </button>
                <button className={styles.primaryBtn} disabled={saving} onClick={handleSave}>
                  Save Changes
                </button>
              </div>
            </div>
          ) : (
            <>
              <div>
                <p className={styles.itemTitle}>{assetName(entry.assetId)}</p>
                <p className={styles.itemMeta}>{entry.beneficiary?.fullName}</p>
                <p className={styles.itemMeta}>
                  {entry.beneficiary?.email} · {entry.beneficiary?.relationship}
                </p>
              </div>
              <button className={styles.linkBtn} onClick={() => startEdit(entry)}>
                Edit
              </button>
            </>
          )}
        </div>
      ))}
    </div>
  );
}

function ExecutorTab({ will, userId, onUpdated }) {
  const [changing, setChanging] = useState(false);

  async function handleChange(executor) {
    const updated = await changeWillExecutor(userId, will.id, executor);
    onUpdated(updated);
    setChanging(false);
  }

  if (changing) {
    return (
      <div>
        <p className={styles.hintSmall}>Choose a replacement executor for this will.</p>
        {EXECUTORS.map((ex) => (
          <div key={ex.id} className={styles.assetRow}>
            <div>
              <p className={styles.itemTitle}>{ex.name}</p>
              <p className={styles.itemMeta}>
                {ex.specialization} · {ex.fee.toLocaleString("vi-VN")} VND
              </p>
            </div>
            <button className={styles.primaryBtn} onClick={() => handleChange(ex)}>
              Select
            </button>
          </div>
        ))}
        <button className={styles.secondaryBtn} onClick={() => setChanging(false)}>
          Cancel
        </button>
      </div>
    );
  }

  return (
    <div>
      <Row label="Name" value={will.executor?.name || "—"} />
      <Row label="Specialization" value={will.executor?.specialization || "—"} />
      <Row label="License" value={will.executor?.license || "—"} />
      <Row label="Rating" value={will.executor ? `⭐ ${will.executor.rating}` : "—"} />
      <Row label="Service Fee" value={will.executor ? `${will.executor.fee.toLocaleString("vi-VN")} VND` : "—"} />
      <Row label="Assignment Status" value={will.status === "ACTIVE" ? "Active ✓" : will.status.replaceAll("_", " ")} />
      <Row label="Payment Status" value={will.payment?.status || "—"} />
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={() => setChanging(true)}>
          Change Executor
        </button>
      </div>
    </div>
  );
}

function DmsTab({ will, userId, onUpdated }) {
  const [editing, setEditing] = useState(false);
  const [checkInDays, setCheckInDays] = useState(will.dms.checkInDays);
  const [reminderDays, setReminderDays] = useState(will.dms.reminderDays);
  const [maxMissed, setMaxMissed] = useState(will.dms.maxMissed);
  const [saving, setSaving] = useState(false);

  async function handleSave() {
    setSaving(true);
    const updated = await updateWillDms(userId, will.id, { checkInDays, reminderDays, maxMissed });
    onUpdated(updated);
    setSaving(false);
    setEditing(false);
  }

  async function handleCheckIn() {
    const updated = await checkInDms(userId, will.id);
    onUpdated(updated);
  }

  async function handleCancelHandover() {
    const updated = await cancelDeathHandover(userId, will.id);
    onUpdated(updated);
  }

  if (editing) {
    return (
      <div>
        <label className={styles.label}>Check-in Interval (days)</label>
        <input className={styles.input} type="number" value={checkInDays} onChange={(e) => setCheckInDays(Number(e.target.value))} />
        <label className={styles.label}>Reminder Before Due (days)</label>
        <input className={styles.input} type="number" value={reminderDays} onChange={(e) => setReminderDays(Number(e.target.value))} />
        <label className={styles.label}>Maximum Missed Check-ins</label>
        <input className={styles.input} type="number" value={maxMissed} onChange={(e) => setMaxMissed(Number(e.target.value))} />
        <div className={styles.actions}>
          <button className={styles.secondaryBtn} onClick={() => setEditing(false)}>
            Cancel
          </button>
          <button className={styles.primaryBtn} disabled={saving} onClick={handleSave}>
            Save Changes
          </button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <Row label="Status" value={will.dms.enabled ? "Active" : "Disabled"} />
      <Row label="Check-in Interval" value={`Every ${will.dms.checkInDays} Days`} />
      <Row label="Reminder Before Due Date" value={`${will.dms.reminderDays} Days`} />
      <Row label="Maximum Missed Check-ins" value={will.dms.maxMissed} />
      <Row label="Missed Check-ins" value={`${will.dms.missedCount ?? 0}/${will.dms.maxMissed}`} />
      <Row label="Last Check-in" value={will.dms.lastCheckIn ? new Date(will.dms.lastCheckIn).toLocaleDateString() : "—"} />

      {will.status === "PENDING_VERIFICATION" && (
        <div className={styles.pendingNote}>
          Your Dead Man's Switch has triggered and a handover case is now open with your Executor.
          If this is a mistake, you can cancel it below.
          <div className={styles.demoRow}>
            <button className={styles.primaryBtn} onClick={handleCancelHandover}>
              I'm Still Active / Cancel Handover
            </button>
          </div>
        </div>
      )}

      <div className={styles.actions}>
        <button
          className={styles.secondaryBtn}
          onClick={handleCheckIn}
          disabled={will.status === "PENDING_VERIFICATION"}
          title={
            will.status === "PENDING_VERIFICATION"
              ? "Use \"I'm Still Active / Cancel Handover\" above to resolve the open case first"
              : undefined
          }
        >
          Check In Now
        </button>
        <button className={styles.primaryBtn} onClick={() => setEditing(true)}>
          Edit Settings
        </button>
      </div>
    </div>
  );
}
