import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import {
  getAssets,
  addAsset,
  getProfile,
  getWill,
  saveWillDraft,
  updateWillBeneficiary,
  submitWill,
  searchBeneficiaryAccount,
  getExecutors,
} from "../../api/vaultApi";
import { ApiError } from "../../api/client";
import styles from "./CreateWill.module.css";
import {AssetFields,EMPTY_FORM} from "../Assets/Assets";

const STEPS = ["Information", "Assets", "Beneficiaries", "Executor", "DMS", "Review"];
const RELATIONSHIPS = ["Daughter", "Son", "Spouse", "Parent", "Sibling", "Friend", "Other"];

export default function CreateWill() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const willId = params.get("willId");

  const [step, setStep] = useState(0);
  const [will, setWill] = useState(null);
  const [assets, setAssets] = useState([]);
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [result, setResult] = useState(null);
  const [executors, setExecutors] = useState([]);

  useEffect(() => {
    bootstrap();
  }, []);

  async function bootstrap() {
    setLoading(true);
    setLoadError("");
    try {
    const [assetsData, profileData] = await Promise.all([getAssets(user?.id), getProfile(user?.id, user)]);
    setAssets(assetsData);
    getExecutors().then(setExecutors).catch(console.error);
    setProfile(profileData);

    let currentWill;
    if (willId) {
      try {
        currentWill = await getWill(user?.id, willId);
      } catch (err) {
        throw new Error(`Could not load this will: ${err.message}`);
      }
    } else {
      // Only local draft at first: no request until the user saves Information.
      currentWill = {id:null,name:"",description:"",assets:[],beneficiaries:[],executor:null,dms:null,status:"DRAFT"};
    }
    setWill(currentWill);
    } catch (err) {
      setLoadError(err.message || "Could not initialize will");
    } finally {
      setLoading(false);
    }
  }

  async function persist(patch) {
    const updated = await saveWillDraft(user?.id, will.id, patch);
    if (!will.id && updated.id) setParams({willId:updated.id},{replace:true});
    setWill(updated);
    return updated;
  }

  function goNext() {
    setStep((s) => Math.min(s + 1, STEPS.length - 1));
  }
  function goBack() {
    setStep((s) => Math.max(s - 1, 0));
  }

  if (loading) {
    return <p className={styles.muted}>Loading...</p>;
  }

  if (loadError) return <p role="alert" className={styles.muted}>{loadError}</p>;
  if (!will) return <p className={styles.muted}>Will unavailable.</p>;

  if (result) {
    return <ResultScreen will={will} />;
  }

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Create New Will</h1>
      <p className={styles.subtitle}>Set up your will and assign your digital assets.</p>

      <div className={styles.stepper}>
        {STEPS.map((label, idx) => (
          <span key={label} className={styles.stepWrap}>
            <button
              type="button"
              className={idx === step ? styles.stepActive : styles.step}
              onClick={() => setStep(idx)}
            >
              {label}
            </button>
            {idx < STEPS.length - 1 && <span className={styles.stepDash}> — </span>}
          </span>
        ))}
      </div>

      <div className={styles.card}>
        {step === 0 && <InfoStep will={will} onSave={persist} onNext={goNext} userFullName={user?.fullName} />}
        {step === 1 && (
          <AssetsStep
            will={will}
            assets={assets}
            userId={user?.id}
            onAssetsChanged={setAssets}
            onSave={persist}
            onNext={goNext}
            onBack={goBack}
          />
        )}
        {step === 2 && (
          <BeneficiariesStep
            will={will}
            assets={assets}
            profile={profile}
            userId={user?.id}
            willId={will.id}
            onUpdated={setWill}
            onNext={goNext}
            onBack={goBack}
          />
        )}
        {step === 3 && (
          <ExecutorStep executors={executors} will={will} onSave={persist} onNext={goNext} onBack={goBack} />
        )}
        {step === 4 && <DmsStep will={will} onSave={persist} onNext={goNext} onBack={goBack} />}
        {step === 5 && (
          <ReviewStep
            will={will}
            assets={assets}
            profile={profile}
            userId={user?.id}
            onBack={goBack}
            onSubmitted={() => navigate(`/dashboard/my-wills/${will.id}`, {replace:true})}
          />
        )}
      </div>
    </div>
  );
}

function InfoStep({ will, onSave, onNext, userFullName }) {
  const [name, setName] = useState(will.name || "");
  const [description, setDescription] = useState(will.description || "");
  const [error,setError]=useState("");
  const [saving, setSaving] = useState(false);

  async function handleNext() {
    setSaving(true);
    try { await onSave({ name, description }); setError(""); onNext(); }
    catch(err) { setError(err.message || "Could not save will"); }
    finally { setSaving(false); }
  }

  async function handleSaveDraft() {
    setSaving(true);
    try { await onSave({ name, description }); setError(""); }
    catch(err) { setError(err.message || "Could not save will"); }
    finally { setSaving(false); }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Will Information</h2>
      {error && <p className={styles.error} role="alert">{error}</p>}
      <label className={styles.label}>Will Name</label>
      <input className={styles.input} value={name} onChange={(e) => setName(e.target.value)} placeholder="Family Legacy 2026" />

      <label className={styles.label}>Description</label>
      <textarea
        className={styles.textarea}
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="Digital assets legacy..."
      />

      <label className={styles.label}>Created By</label>
      <p className={styles.staticValue}>{userFullName || "—"}</p>

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} disabled={saving} onClick={handleSaveDraft}>
          Save Draft
        </button>
        <button className={styles.primaryBtn} disabled={saving || !name.trim()} onClick={handleNext}>
          Next →
        </button>
      </div>
    </div>
  );
}

function AssetsStep({ will, assets, userId, onAssetsChanged, onSave, onNext, onBack }) {
  const [selected, setSelected] = useState(new Set((will.assets || []).map((a) => a.assetId)));
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState({...EMPTY_FORM});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  function toggle(assetId) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(assetId)) next.delete(assetId);
      else next.add(assetId);
      return next;
    });
  }

  async function handleAddAsset(e) {
    e.preventDefault();
    if (!form.name.trim()) {
      setError("Asset name is required");
      return;
    }
    setSaving(true);
    try {
      const asset = await addAsset(userId, form);
      const updatedAssets = [...assets, asset];
      onAssetsChanged(updatedAssets);
      setSelected((prev) => new Set(prev).add(asset.id));
      setShowModal(false);
      setForm({...EMPTY_FORM});
      setError("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not add asset");
    } finally {
      setSaving(false);
    }
  }

  async function handleNext() {
    const existing = will.assets || [];
    const nextAssets = Array.from(selected).map((assetId) => {
      const found = existing.find((a) => a.assetId === assetId);
      return found || { assetId, beneficiary: null };
    });
    setSaving(true);
    try { await onSave({ assets: nextAssets }); setError(""); onNext(); }
    catch(err) { setError(err.message || "Could not save assets"); }
    finally { setSaving(false); }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Select Assets</h2>
      <p className={styles.hint}>Select the assets you want to include in this will.</p>

      {assets.length === 0 && <p className={styles.hint}>No assets yet — add one below.</p>}

      <div className={styles.checkList}>
        {assets.map((asset) => (
          <label key={asset.id} className={styles.checkRow}>
            <input type="checkbox" checked={selected.has(asset.id)} onChange={() => toggle(asset.id)} />
            <span>
              <strong>{asset.name}</strong>
              <span className={styles.checkMeta}>
                {asset.type}
              </span>
            </span>
          </label>
        ))}
      </div>

      <button type="button" className={styles.linkBtn} onClick={() => setShowModal(true)}>
        + Add New Asset
      </button>

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>
          ← Back
        </button>
        <button className={styles.primaryBtn} disabled={saving || selected.size === 0} onClick={handleNext}>
          Next →
        </button>
      </div>

      {showModal && (
        <div className={styles.overlay} onClick={() => setShowModal(false)}>
          <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleAddAsset}>
            <h3 className={styles.modalTitle}>Add Asset</h3>
            <AssetFields form={form} setForm={setForm} />
            {error && <p className={styles.error}>{error}</p>}
            <div className={styles.actions}>
              <button type="button" className={styles.secondaryBtn} onClick={() => setShowModal(false)}>
                Cancel
              </button>
              <button type="submit" className={styles.primaryBtn} disabled={saving}>
                {saving ? "Adding..." : "Add Asset"}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function BeneficiariesStep({ will, assets, profile, userId, willId, onUpdated, onNext, onBack }) {
  const [activeAssetId, setActiveAssetId] = useState(null);
  const isVerified = profile.identityStatus === "VERIFIED";

  const willAssets = will.assets || [];
  const allAssigned = willAssets.length > 0 && willAssets.every((a) => a.beneficiary);

  function assetName(assetId) {
    return assets.find((a) => a.id === assetId)?.name || "Asset";
  }

  async function handleAssign(assetId, beneficiary) {
    try {
      if (!beneficiary) throw new Error("Removing beneficiaries requires a dedicated backend endpoint.");
    const updated = await updateWillBeneficiary(userId, willId, assetId, beneficiary);
      onUpdated(updated);
      setActiveAssetId(null);
    } catch(err) { throw err; }
  }

  if (!isVerified) {
    return (
      <div>
        <h2 className={styles.stepHeading}>Assign Beneficiaries</h2>
        <div className={styles.warnBox}>
          <h3 className={styles.warnTitle}>Identity Verification Required</h3>
          <p className={styles.hint}>You must verify your identity before assigning beneficiaries.</p>
          <div className={styles.actions}>
            <button className={styles.secondaryBtn} onClick={onBack}>
              ← Back
            </button>
            <a
              className={styles.primaryBtn}
              href={`/dashboard/verify-identity?returnTo=${encodeURIComponent(
                `/dashboard/create-will?willId=${willId}`
              )}`}
            >
              Verify Identity
            </a>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Assign Beneficiaries</h2>
      <p className={styles.infoNote}>ℹ Beneficiaries will not be notified until the handover process is activated.</p>

      {willAssets.map((entry) => (
        <div key={entry.assetId} className={styles.beneficiaryCard}>
          <h4 className={styles.beneficiaryAssetName}>{assetName(entry.assetId)}</h4>
          {entry.beneficiary ? (
            <div className={styles.beneficiaryInfo}>
              <p className={styles.beneficiaryName}>{entry.beneficiary.fullName}</p>
              <p className={styles.beneficiaryMeta}>
                {entry.beneficiary.email} · {entry.beneficiary.phone} · {entry.beneficiary.relationship}
                {entry.beneficiary.identityNumber ? ` · CCCD: ${entry.beneficiary.identityNumber}` : ""}
              </p>
              <p className={styles.beneficiaryStatus}>Status: {entry.beneficiary.accountId ? "Designated (linked)" : "Designated"}</p>
              <button className={styles.linkBtn} onClick={() => setActiveAssetId(entry.assetId)}>
                Change
              </button>{" "}
              <button className={styles.linkBtnDanger} onClick={() => setActiveAssetId(entry.assetId)}>
                Change beneficiary
              </button>
            </div>
          ) : (
            <button className={styles.linkBtn} onClick={() => setActiveAssetId(entry.assetId)}>
              + Add Beneficiary
            </button>
          )}
        </div>
      ))}

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>
          ← Back
        </button>
        <button className={styles.primaryBtn} disabled={!allAssigned} onClick={onNext}>
          Next →
        </button>
      </div>

      {activeAssetId && (
        <BeneficiaryModal
          onClose={() => setActiveAssetId(null)}
          onAssign={(beneficiary) => handleAssign(activeAssetId, beneficiary)}
        />
      )}
    </div>
  );
}

function BeneficiaryModal({ onClose, onAssign }) {
  const [mode, setMode] = useState("search");
  const [query, setQuery] = useState("");
  const [found, setFound] = useState(undefined);
  const [relationship, setRelationship] = useState("Daughter");
  const [newForm, setNewForm] = useState({ fullName: "", email: "", phone: "", identityNumber: "" });
  const [lookupError,setLookupError]=useState("");
  const [searching,setSearching]=useState(false);
  const [assigning,setAssigning]=useState(false);

  function handleQueryChange(value) { setQuery(value); setFound(undefined); setLookupError(""); }
  async function handleSearch() {
    if (!query.trim()) return;
    setSearching(true); setLookupError(""); setFound(undefined);
    try { setFound(await searchBeneficiaryAccount(query.trim())); }
    catch(err) { if(err.status===404) setFound(null); else setLookupError(err.message||"Search failed"); }
    finally { setSearching(false); }
  }
  async function assign(person) {
    setAssigning(true);setLookupError("");
    try { await onAssign(person); }
    catch(err) { setLookupError(err.message||"Cannot assign beneficiary"); }
    finally { setAssigning(false); }
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.tabRow}>
          <button
            className={mode === "search" ? styles.tabActive : styles.tab}
            onClick={() => setMode("search")}
          >
            Existing Account
          </button>
          <button className={mode === "new" ? styles.tabActive : styles.tab} onClick={() => setMode("new")}>
            New Beneficiary
          </button>
        </div>

        {mode === "search" ? (
          <>
            <label className={styles.label}>Search Beneficiary</label>
            <div className={styles.searchRow}>
              <input
                className={styles.input}
                value={query}
                onChange={(e) => handleQueryChange(e.target.value)}
                placeholder="Email or phone number"
              />
              <button type="button" className={styles.secondaryBtn} disabled={searching} onClick={handleSearch}>
                Search
              </button>
            </div>
            {lookupError && <p className={styles.error} role="alert">{lookupError}</p>}
            {found === null && <p className={styles.hint}>No account found with that email or phone number.</p>}
            {found && (
              <div className={styles.foundCard}>
                <p className={styles.beneficiaryName}>{found.fullName}</p>
                <p className={styles.beneficiaryMeta}>
                  {found.maskedEmail || found.email} · {found.maskedPhoneNumber || found.phone}
                  {found.identityNumber ? ` · CCCD: ${found.identityNumber}` : ""}
                </p>
                <label className={styles.label}>Relationship</label>
                <select className={styles.input} value={relationship} onChange={(e) => setRelationship(e.target.value)}>
                  {RELATIONSHIPS.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </select>
                <button
                  className={styles.primaryBtn}
                  onClick={() =>
                    assign({ ...found, relationship, accountId: found.id })
                  }
                >
                  Select
                </button>
              </div>
            )}
          </>
        ) : (
          <>
            <label className={styles.label}>Full Name</label>
            <input
              className={styles.input}
              value={newForm.fullName}
              onChange={(e) => setNewForm({ ...newForm, fullName: e.target.value })}
            />
            <label className={styles.label}>Email</label>
            <input className={styles.input} value={newForm.email} onChange={(e) => setNewForm({ ...newForm, email: e.target.value })} />
            <label className={styles.label}>Phone Number</label>
            <input className={styles.input} value={newForm.phone} onChange={(e) => setNewForm({ ...newForm, phone: e.target.value })} />
            <label className={styles.label}>National ID (CCCD)</label>
            <input
              className={styles.input}
              value={newForm.identityNumber}
              onChange={(e) => setNewForm({ ...newForm, identityNumber: e.target.value })}
              placeholder="Optional"
            />
            <label className={styles.label}>Relationship</label>
            <select className={styles.input} value={relationship} onChange={(e) => setRelationship(e.target.value)}>
              {RELATIONSHIPS.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
            <div className={styles.actions}>
              <button className={styles.secondaryBtn} onClick={onClose}>
                Cancel
              </button>
              <button
                className={styles.primaryBtn}
                disabled={!newForm.fullName || !newForm.email}
                onClick={() =>
                  assign({ ...newForm, relationship, accountId: null })
                }
              >
                Assign
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function ExecutorStep({ will, onSave, onNext, onBack, executors }) {
  const [selectedId, setSelectedId] = useState(will.executor?.id || null);
  const [saving, setSaving] = useState(false);
  const selected = executors.find((e) => e.id === selectedId);
  const [error,setError]=useState("");

  async function handleNext() {
    setSaving(true);
    try { await onSave({ executor: selected }); setError(""); onNext(); }
    catch(err) { setError(err.message || "Could not assign executor"); }
    finally { setSaving(false); }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Select Executor</h2>
      {error && <p role="alert" className={styles.error}>{error}</p>}
      <div className={styles.executorList}>
        {executors.map((ex) => (
          <div key={ex.id} className={styles.executorCard}>
            <div>
              <h4 className={styles.beneficiaryName}>
                {ex.name} <span className={styles.availableTag}>● Available</span>
              </h4>
              <p className={styles.beneficiaryMeta}>{ex.specialization}</p>
              <p className={styles.beneficiaryMeta}>License: {ex.license}</p>
              <p className={styles.beneficiaryMeta}>
                ⭐ {ex.rating} · {ex.reviews} Reviews · {ex.experienceYears} Years Experience · {ex.completedCases} Completed Cases
              </p>
              <p className={styles.feeText}>Service Fee: {(ex.fee ?? 0).toLocaleString("vi-VN")} VND</p>
            </div>
            <button
              className={selectedId === ex.id ? styles.primaryBtn : styles.secondaryBtn}
              onClick={() => setSelectedId(ex.id)}
            >
              {selectedId === ex.id ? "Selected" : "Select"}
            </button>
          </div>
        ))}
      </div>

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>
          ← Back
        </button>
        <button className={styles.primaryBtn} disabled={!selected || saving} onClick={handleNext}>
          Next →
        </button>
      </div>
    </div>
  );
}

function DmsStep({ will, onSave, onNext, onBack }) {
  const checkInDaysInit = will.dms?.checkInDays ?? 30;
  const reminderDaysInit = will.dms?.reminderDays ?? 3;
  const maxMissedInit = will.dms?.maxMissed ?? 2;
  const [checkInDays, setCheckInDays] = useState(checkInDaysInit);
  const [reminderDays, setReminderDays] = useState(reminderDaysInit);
  const [maxMissed, setMaxMissed] = useState(maxMissedInit);
  const [error,setError]=useState("");
  const [saving, setSaving] = useState(false);

  async function handleNext() {
    setSaving(true);
    try { await onSave({ dms: { ...will.dms, enabled: true, checkInDays, reminderDays, maxMissed } }); setError(""); onNext(); }
    catch(err) { setError(err.message || "Could not save DMS"); }
    finally { setSaving(false); }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Dead Man's Switch</h2>
      {error && <p role="alert" className={styles.error}>{error}</p>}

      <div className={styles.checkRow}>
        <span className={styles.dmsLockedBadge}>✓ Always On</span>
        <span>
          Dead Man's Switch is enabled by default for every will and cannot be disabled — it's what protects your
          assets if you become unreachable.
        </span>
      </div>

      <label className={styles.label}>Check-in Interval</label>
      <select className={styles.input} value={checkInDays} onChange={(e) => setCheckInDays(Number(e.target.value))}>
        {[15, 30, 60, 90].map((d) => (
          <option key={d} value={d}>
            {d} Days
          </option>
        ))}
      </select>

      <label className={styles.label}>Reminder Before Due Date</label>
      <select className={styles.input} value={reminderDays} onChange={(e) => setReminderDays(Number(e.target.value))}>
        {[1, 3, 7].map((d) => (
          <option key={d} value={d}>
            {d} Days
          </option>
        ))}
      </select>

      <label className={styles.label}>Maximum Missed Check-ins</label>
      <select className={styles.input} value={maxMissed} onChange={(e) => setMaxMissed(Number(e.target.value))}>
        {[1, 2, 3].map((d) => (
          <option key={d} value={d}>
            {d}
          </option>
        ))}
      </select>

      <p className={styles.infoNote}>
        ℹ Missing the configured check-ins will notify the Executor and trigger the verification process. DMS does not
        authorize asset handover on its own.
      </p>

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>
          ← Back
        </button>
        <button className={styles.primaryBtn} disabled={saving} onClick={handleNext}>
          Next →
        </button>
      </div>
    </div>
  );
}

function ReviewStep({ will, assets, profile, userId, onBack, onSubmitted }) {
  const [showPayment, setShowPayment] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError,setSubmitError]=useState("");

  function assetName(assetId) {
    return assets.find((a) => a.id === assetId)?.name || "Asset";
  }

  async function handleConfirmSubmit() {
    setSubmitting(true);
    try { await submitWill(userId, will.id); setSubmitError(""); setShowPayment(false); onSubmitted(); }
    catch(err) { setSubmitError(err.message || "Will submission failed"); }
    finally { setSubmitting(false); }
  }

  const isVerified = profile.identityStatus === "VERIFIED";

  return (
    <div>
      <h2 className={styles.stepHeading}>Review Your Will</h2>
      <h3 className={styles.reviewWillName}>{will.name || "Untitled Will"}</h3>

      <div className={styles.reviewBlock}>
        <p className={styles.reviewLabel}>Assets ({(will.assets || []).length})</p>
        {(will.assets || []).map((a) => (
          <p key={a.assetId} className={styles.reviewLine}>
            {assetName(a.assetId)} → {a.beneficiary?.fullName || "—"}
          </p>
        ))}
      </div>

      <div className={styles.reviewBlock}>
        <p className={styles.reviewLabel}>Executor</p>
        <p className={styles.reviewLine}>
          {will.executor?.name} · Service Fee: {will.executor?.fee?.toLocaleString("vi-VN")} VND
        </p>
      </div>

      <div className={styles.reviewBlock}>
        <p className={styles.reviewLabel}>Dead Man's Switch</p>
        <p className={styles.reviewLine}>
          {will.dms?.enabled ? "Enabled" : "Disabled"} · Check-in every {will.dms?.checkInDays} days · Max missed{" "}
          {will.dms?.maxMissed}
        </p>
      </div>

      <div className={styles.reviewBlock}>
        <p className={styles.reviewLabel}>Identity Verification</p>
        <p className={isVerified ? styles.reviewOk : styles.reviewWarn}>{isVerified ? "✓ VERIFIED" : "⚠ NOT VERIFIED"}</p>
      </div>

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>
          ← Back
        </button>
        <button
          className={styles.primaryBtn}
          disabled={!isVerified || !will.executor}
          onClick={() => setShowPayment(true)}
        >
          Submit Will
        </button>
      </div>

      {showPayment && (
        <div className={styles.overlay} onClick={() => setShowPayment(false)}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h3 className={styles.modalTitle}>Confirm Will Submission</h3>
            {submitError && <p className={styles.error} role="alert">{submitError}</p>}
            <p className={styles.reviewLine}>Executor: {will.executor?.name}</p>
            <p className={styles.reviewLine}>Service Fee: {will.executor?.fee?.toLocaleString("vi-VN")} VND</p>
            <p className={styles.hint}>The will will be sent to the assigned Executor for acceptance. Payment integration is not available yet.</p>
            <div className={styles.actions}>
              <button className={styles.secondaryBtn} onClick={() => setShowPayment(false)}>
                Cancel
              </button>
              <button className={styles.primaryBtn} disabled={submitting} onClick={handleConfirmSubmit}>
                {submitting ? "Processing..." : "Confirm & Submit"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function ResultScreen({ will }) {
  const navigate = useNavigate();

  return (
    <div className={styles.card}>
      <div className={styles.resultCheck}>✓</div>
      <h2 className={styles.stepHeading}>Will Submitted</h2>
      <p className={styles.reviewWillName}>{will.name}</p>
      <p className={styles.reviewLine}>Status: PENDING EXECUTOR ACCEPTANCE</p>
      <p className={styles.reviewLine}>Executor: {will.executor?.name} — waiting for acceptance</p>
      
      <p className={styles.hint}>
        {will.executor?.name} has been assigned this will and has
        {will.hireRequest?.deadlineAt ? ` until ${new Date(will.hireRequest.deadlineAt).toLocaleDateString("en-US")}` : " time"}
        {" "}to accept or decline. You'll see the outcome on this will's details page.
      </p>

      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={() => navigate("/dashboard/my-wills")}>
          My Wills
        </button>
      </div>
    </div>
  );
}
