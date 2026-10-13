import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { getAssets } from "../../api/assetsApi";
import { getMe, searchAccount } from "../../api/accountsApi";
import {
  createWill,
  updateWill,
  getWill,
  addWillAsset,
  addBeneficiary,
  assignAsset,
  getExecutors,
  assignExecutor,
  configureDms,
  submitWill,
} from "../../api/willsApi";
import { ApiError } from "../../api/client";
import styles from "./CreateWill.module.css";

const STEPS = ["Information", "Assets", "Beneficiaries", "Executor", "DMS", "Review"];
const RELATIONSHIPS = ["Daughter", "Son", "Spouse", "Parent", "Sibling", "Friend", "Other"];

function emptyWill() {
  return {
    id: null,
    title: "",
    description: "",
    status: "DRAFT",
    assets: [],
    beneficiaries: [],
    assignments: [],
    executor: null,
    dms: null,
  };
}

function beneficiaryForAsset(will, willAssetId) {
  const assignment = (will.assignments || []).find((x) => x.willAssetId === willAssetId);
  if (!assignment) return null;
  return (will.beneficiaries || []).find((x) => x.id === assignment.beneficiaryDesignationId) || null;
}

export default function CreateWill() {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const willId = params.get("willId");

  const [step, setStep] = useState(0);
  const [will, setWill] = useState(willId ? null : emptyWill());
  const [assets, setAssets] = useState([]);
  const [executors, setExecutors] = useState([]);
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [pageError, setPageError] = useState("");
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    bootstrap();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [willId]);

  async function bootstrap() {
    setLoading(true);
    setPageError("");
    try {
      const [assetData, executorData, profileData] = await Promise.all([
        getAssets(),
        getExecutors(),
        getMe(),
      ]);
      setAssets(assetData);
      setExecutors(executorData || []);
      setProfile(profileData);

      if (willId) {
        setWill(await getWill(willId));
      } else {
        setWill((current) => current || emptyWill());
      }
    } catch (err) {
      setPageError(err instanceof ApiError ? err.message : err.message || "Could not load will data");
    } finally {
      setLoading(false);
    }
  }

  async function refreshWill(id = will?.id) {
    if (!id) return null;
    const data = await getWill(id);
    setWill(data);
    return data;
  }

  async function saveInfo({ title, description }) {
    let summary;
    if (!will?.id) {
      summary = await createWill({ title, description });
      setParams({ willId: summary.id }, { replace: true });
    } else {
      summary = await updateWill(will.id, { title, description });
    }
    return refreshWill(summary.id);
  }

  if (loading || !will) return <p className={styles.muted}>Loading...</p>;

  if (submitted) {
    return (
      <div className={styles.card}>
        <div className={styles.resultCheck}>✓</div>
        <h2 className={styles.stepHeading}>Will Submitted</h2>
        <p className={styles.reviewWillName}>{will.title}</p>
        <p className={styles.reviewLine}>Status: {will.status}</p>
        <p className={styles.reviewLine}>Executor: {will.executor?.executorName || "—"}</p>
        <div className={styles.actions}>
          <button className={styles.primaryBtn} onClick={() => navigate("/dashboard/my-wills")}>My Wills</button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Create New Will</h1>
      <p className={styles.subtitle}>Real backend flow: Draft → Asset → Beneficiary → Executor → DMS → Submit.</p>
      {pageError && <p className={styles.error}>{pageError}</p>}

      <div className={styles.stepper}>
        {STEPS.map((label, idx) => (
          <span key={label} className={styles.stepWrap}>
            <button
              type="button"
              disabled={!will.id && idx > 0}
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
        {step === 0 && (
          <InfoStep
            will={will}
            userFullName={profile?.fullName}
            onSave={saveInfo}
            onNext={() => setStep(1)}
          />
        )}
        {step === 1 && (
          <AssetsStep
            will={will}
            assets={assets}
            onRefresh={refreshWill}
            onBack={() => setStep(0)}
            onNext={() => setStep(2)}
          />
        )}
        {step === 2 && (
          <BeneficiariesStep
            will={will}
            onRefresh={refreshWill}
            onBack={() => setStep(1)}
            onNext={() => setStep(3)}
          />
        )}
        {step === 3 && (
          <ExecutorStep
            will={will}
            executors={executors}
            onRefresh={refreshWill}
            onBack={() => setStep(2)}
            onNext={() => setStep(4)}
          />
        )}
        {step === 4 && (
          <DmsStep
            will={will}
            onRefresh={refreshWill}
            onBack={() => setStep(3)}
            onNext={() => setStep(5)}
          />
        )}
        {step === 5 && (
          <ReviewStep
            will={will}
            profile={profile}
            onBack={() => setStep(4)}
            onSubmitted={(updated) => {
              setWill(updated);
              setSubmitted(true);
            }}
          />
        )}
      </div>
    </div>
  );
}

function InfoStep({ will, userFullName, onSave, onNext }) {
  const [title, setTitle] = useState(will.title || "");
  const [description, setDescription] = useState(will.description || "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function save(goNext) {
    if (!title.trim()) return setError("Will name is required");
    setSaving(true);
    setError("");
    try {
      await onSave({ title: title.trim(), description });
      if (goNext) onNext();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not save draft");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Will Information</h2>
      <label className={styles.label}>Will Name</label>
      <input className={styles.input} value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Family Legacy 2026" />
      <label className={styles.label}>Description</label>
      <textarea className={styles.textarea} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Digital assets legacy..." />
      <label className={styles.label}>Created By</label>
      <p className={styles.staticValue}>{userFullName || "—"}</p>
      {error && <p className={styles.error}>{error}</p>}
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} disabled={saving} onClick={() => save(false)}>Save Draft</button>
        <button className={styles.primaryBtn} disabled={saving || !title.trim()} onClick={() => save(true)}>Next →</button>
      </div>
    </div>
  );
}

function AssetsStep({ will, assets, onRefresh, onNext, onBack }) {
  const existingIds = useMemo(() => new Set((will.assets || []).map((x) => x.assetId)), [will.assets]);
  const [selected, setSelected] = useState(new Set(existingIds));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  function toggle(id) {
    if (existingIds.has(id)) return;
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async function handleNext() {
    if (selected.size === 0) return setError("Select at least one asset");
    setSaving(true);
    setError("");
    try {
      const newIds = [...selected].filter((id) => !existingIds.has(id));
      for (const assetId of newIds) await addWillAsset(will.id, assetId);
      await onRefresh();
      onNext();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not add assets to will");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Select Assets</h2>
      <p className={styles.hint}>Existing assets already added to this draft are locked in the current V1 API.</p>
      {assets.length === 0 ? <p className={styles.hint}>Create an Asset first from the Assets page.</p> : (
        <div className={styles.checkList}>
          {assets.map((asset) => (
            <label key={asset.id} className={styles.checkRow}>
              <input type="checkbox" checked={selected.has(asset.id)} disabled={existingIds.has(asset.id)} onChange={() => toggle(asset.id)} />
              <span><strong>{asset.name}</strong><span className={styles.checkMeta}>{asset.type} · {asset.verificationStatus}</span></span>
            </label>
          ))}
        </div>
      )}
      {error && <p className={styles.error}>{error}</p>}
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>← Back</button>
        <button className={styles.primaryBtn} disabled={saving || selected.size === 0} onClick={handleNext}>{saving ? "Saving..." : "Next →"}</button>
      </div>
    </div>
  );
}

function BeneficiariesStep({ will, onRefresh, onNext, onBack }) {
  const [activeAsset, setActiveAsset] = useState(null);
  const [error, setError] = useState("");

  const allAssigned = (will.assets || []).length > 0 && (will.assets || []).every((a) => beneficiaryForAsset(will, a.willAssetId));

  async function handleAssign(willAsset, beneficiaryInput) {
    setError("");
    try {
      let designation = null;
      if (beneficiaryInput.beneficiaryAccountId) {
        designation = (will.beneficiaries || []).find((b) => b.beneficiaryAccountId === beneficiaryInput.beneficiaryAccountId);
      } else if (beneficiaryInput.email) {
        designation = (will.beneficiaries || []).find((b) => b.email?.toLowerCase() === beneficiaryInput.email.toLowerCase());
      }

      if (!designation) designation = await addBeneficiary(will.id, beneficiaryInput);
      await assignAsset(will.id, willAsset.willAssetId, designation.id);
      await onRefresh();
      setActiveAsset(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not assign beneficiary");
    }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Assign Beneficiaries</h2>
      <p className={styles.hint}>Search uses one input: email or phone. The backend returns masked contact data.</p>
      {(will.assets || []).map((entry) => {
        const b = beneficiaryForAsset(will, entry.willAssetId);
        return (
          <div key={entry.willAssetId} className={styles.beneficiaryCard}>
            <h4 className={styles.beneficiaryAssetName}>{entry.assetName}</h4>
            {b ? (
              <div className={styles.beneficiaryInfo}>
                <p className={styles.beneficiaryName}>{b.fullName}</p>
                <p className={styles.beneficiaryMeta}>{b.email} · {b.phone || "No phone"} · {b.relationship || "—"}</p>
                <p className={styles.beneficiaryStatus}>Status: {b.beneficiaryAccountId ? "Designated (linked account)" : "Designated (manual)"}</p>
              </div>
            ) : (
              <button className={styles.linkBtn} onClick={() => setActiveAsset(entry)}>+ Add Beneficiary</button>
            )}
          </div>
        );
      })}
      {error && <p className={styles.error}>{error}</p>}
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>← Back</button>
        <button className={styles.primaryBtn} disabled={!allAssigned} onClick={onNext}>Next →</button>
      </div>
      {activeAsset && <BeneficiaryModal onClose={() => setActiveAsset(null)} onAssign={(payload) => handleAssign(activeAsset, payload)} />}
    </div>
  );
}

function BeneficiaryModal({ onClose, onAssign }) {
  const [mode, setMode] = useState("search");
  const [query, setQuery] = useState("");
  const [found, setFound] = useState(undefined);
  const [relationship, setRelationship] = useState("Sibling");
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState("");
  const [newForm, setNewForm] = useState({ fullName: "", email: "", phone: "" });

  async function handleSearch() {
    setSearching(true);
    setError("");
    setFound(undefined);
    try {
      setFound(await searchAccount(query));
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) setFound(null);
      else setError(err.message || "Search failed");
    } finally {
      setSearching(false);
    }
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.tabRow}>
          <button className={mode === "search" ? styles.tabActive : styles.tab} onClick={() => setMode("search")}>Existing Account</button>
          <button className={mode === "new" ? styles.tabActive : styles.tab} onClick={() => setMode("new")}>New Beneficiary</button>
        </div>

        {mode === "search" ? (
          <>
            <label className={styles.label}>Email or Phone</label>
            <div className={styles.searchRow}>
              <input className={styles.input} value={query} onChange={(e) => setQuery(e.target.value)} placeholder="minhanh@... or +849..." onKeyDown={(e) => e.key === "Enter" && handleSearch()} />
              <button type="button" className={styles.secondaryBtn} disabled={searching || !query.trim()} onClick={handleSearch}>{searching ? "Searching..." : "Search"}</button>
            </div>
            {found === null && <p className={styles.hint}>No LegacyVault account found. Use New Beneficiary.</p>}
            {error && <p className={styles.error}>{error}</p>}
            {found && (
              <div className={styles.foundCard}>
                <p className={styles.beneficiaryName}>{found.fullName}</p>
                <p className={styles.beneficiaryMeta}>{found.maskedEmail || "—"} · {found.maskedPhoneNumber || "—"}</p>
                <p className={styles.beneficiaryStatus}>Identity: {found.identityStatus}</p>
                <label className={styles.label}>Relationship</label>
                <select className={styles.input} value={relationship} onChange={(e) => setRelationship(e.target.value)}>
                  {RELATIONSHIPS.map((r) => <option key={r} value={r}>{r}</option>)}
                </select>
                <button className={styles.primaryBtn} onClick={() => onAssign({ beneficiaryAccountId: found.id, relationship })}>Select</button>
              </div>
            )}
          </>
        ) : (
          <>
            <label className={styles.label}>Full Name</label>
            <input className={styles.input} value={newForm.fullName} onChange={(e) => setNewForm({ ...newForm, fullName: e.target.value })} />
            <label className={styles.label}>Email</label>
            <input className={styles.input} value={newForm.email} onChange={(e) => setNewForm({ ...newForm, email: e.target.value })} />
            <label className={styles.label}>Phone Number</label>
            <input className={styles.input} value={newForm.phone} onChange={(e) => setNewForm({ ...newForm, phone: e.target.value })} />
            <label className={styles.label}>Relationship</label>
            <select className={styles.input} value={relationship} onChange={(e) => setRelationship(e.target.value)}>
              {RELATIONSHIPS.map((r) => <option key={r} value={r}>{r}</option>)}
            </select>
            <div className={styles.actions}>
              <button className={styles.secondaryBtn} onClick={onClose}>Cancel</button>
              <button
                className={styles.primaryBtn}
                disabled={!newForm.fullName.trim() || !newForm.email.trim()}
                onClick={() => onAssign({ beneficiaryAccountId: null, ...newForm, relationship })}
              >Assign</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function ExecutorStep({ will, executors, onRefresh, onNext, onBack }) {
  const [selectedId, setSelectedId] = useState(will.executor?.executorAccountId || "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function handleNext() {
    if (!selectedId) return;
    setSaving(true);
    setError("");
    try {
      await assignExecutor(will.id, selectedId);
      await onRefresh();
      onNext();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not assign executor");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Select Executor</h2>
      {executors.length === 0 && <p className={styles.hint}>No verified executor seed found.</p>}
      <div className={styles.executorList}>
        {executors.map((ex) => (
          <div key={ex.accountId} className={styles.executorCard}>
            <div>
              <h4 className={styles.beneficiaryName}>{ex.fullName} <span className={styles.availableTag}>● Verified</span></h4>
              <p className={styles.beneficiaryMeta}>⭐ {ex.rating} · {ex.experienceYears} Years · {ex.completedCases} Completed Cases</p>
              {ex.bio && <p className={styles.beneficiaryMeta}>{ex.bio}</p>}
            </div>
            <button className={selectedId === ex.accountId ? styles.primaryBtn : styles.secondaryBtn} onClick={() => setSelectedId(ex.accountId)}>
              {selectedId === ex.accountId ? "Selected" : "Select"}
            </button>
          </div>
        ))}
      </div>
      {error && <p className={styles.error}>{error}</p>}
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>← Back</button>
        <button className={styles.primaryBtn} disabled={!selectedId || saving} onClick={handleNext}>{saving ? "Saving..." : "Next →"}</button>
      </div>
    </div>
  );
}

function DmsStep({ will, onRefresh, onNext, onBack }) {
  const [checkInIntervalDays, setCheckInIntervalDays] = useState(will.dms?.checkInIntervalDays ?? 30);
  const [reminderBeforeDays, setReminderBeforeDays] = useState(will.dms?.reminderBeforeDays ?? 3);
  const [maxMissedCheckIns, setMaxMissedCheckIns] = useState(will.dms?.maxMissedCheckIns ?? 3);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function handleNext() {
    setSaving(true);
    setError("");
    try {
      await configureDms(will.id, { enabled: true, checkInIntervalDays, reminderBeforeDays, maxMissedCheckIns });
      await onRefresh();
      onNext();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not configure DMS");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Dead Man's Switch</h2>
      <div className={styles.checkRow}><span className={styles.dmsLockedBadge}>✓ Enabled</span><span>DMS becomes ACTIVE when the Will is submitted.</span></div>
      <label className={styles.label}>Check-in Interval</label>
      <select className={styles.input} value={checkInIntervalDays} onChange={(e) => setCheckInIntervalDays(Number(e.target.value))}>
        {[15, 30, 60, 90].map((d) => <option key={d} value={d}>{d} Days</option>)}
      </select>
      <label className={styles.label}>Reminder Before Due Date</label>
      <select className={styles.input} value={reminderBeforeDays} onChange={(e) => setReminderBeforeDays(Number(e.target.value))}>
        {[1, 3, 7].filter((d) => d < checkInIntervalDays).map((d) => <option key={d} value={d}>{d} Days</option>)}
      </select>
      <label className={styles.label}>Maximum Missed Check-ins</label>
      <select className={styles.input} value={maxMissedCheckIns} onChange={(e) => setMaxMissedCheckIns(Number(e.target.value))}>
        {[1, 2, 3].map((d) => <option key={d} value={d}>{d}</option>)}
      </select>
      {error && <p className={styles.error}>{error}</p>}
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>← Back</button>
        <button className={styles.primaryBtn} disabled={saving} onClick={handleNext}>{saving ? "Saving..." : "Next →"}</button>
      </div>
    </div>
  );
}

function ReviewStep({ will, profile, onBack, onSubmitted }) {
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit() {
    setSubmitting(true);
    setError("");
    try {
      onSubmitted(await submitWill(will.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not submit will");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <h2 className={styles.stepHeading}>Review Your Will</h2>
      <h3 className={styles.reviewWillName}>{will.title}</h3>
      <div className={styles.reviewBlock}>
        <p className={styles.reviewLabel}>Assets ({will.assets?.length || 0})</p>
        {(will.assets || []).map((a) => {
          const b = beneficiaryForAsset(will, a.willAssetId);
          return <p key={a.willAssetId} className={styles.reviewLine}>{a.assetName} → {b?.fullName || "—"}</p>;
        })}
      </div>
      <div className={styles.reviewBlock}><p className={styles.reviewLabel}>Executor</p><p className={styles.reviewLine}>{will.executor?.executorName || "—"}</p></div>
      <div className={styles.reviewBlock}><p className={styles.reviewLabel}>DMS</p><p className={styles.reviewLine}>{will.dms ? `Every ${will.dms.checkInIntervalDays} days · Max missed ${will.dms.maxMissedCheckIns}` : "—"}</p></div>
      <div className={styles.reviewBlock}><p className={styles.reviewLabel}>Identity</p><p className={profile?.identityStatus === "VERIFIED" ? styles.reviewOk : styles.reviewWarn}>{profile?.identityStatus || "UNVERIFIED"} (not enforced by current Will task API)</p></div>
      {error && <p className={styles.error}>{error}</p>}
      <div className={styles.actions}>
        <button className={styles.secondaryBtn} onClick={onBack}>← Back</button>
        <button className={styles.primaryBtn} disabled={submitting} onClick={handleSubmit}>{submitting ? "Submitting..." : "Submit Will"}</button>
      </div>
    </div>
  );
}
