import { useEffect, useState, useRef } from "react";
import { useAuth } from "../../context/AuthContext";
import { getAssets, addAsset, updateAsset, getAssetSecret, deleteAsset, getWills } from "../../api/vaultApi";
import { verifyAsset } from "../../api/assetVerifyApi";
import { ApiError } from "../../api/client";
import {
  ACCOUNT_PROVIDERS,
  ENABLED_ASSET_TYPES,
  isVerifiable,
  providerLabel,
  verificationInfo,
} from "../../utils/assetCatalog";
import { buildAssetLocks } from "../../utils/willHelpers";
import ConfirmDiscardDialog from "../../components/ConfirmDiscardDialog/ConfirmDiscardDialog";
import { MAX_FILE_BYTES, formatBytes, readAssetFile, downloadAssetFile, assetFileFromSecret } from "../../utils/assetFile";
import { useBackdropClose } from "../../utils/useBackdropClose";
import styles from "./Assets.module.css";

// Re-exported so CreateWill keeps working with the same import.
export const ASSET_TYPES = ENABLED_ASSET_TYPES;
export const ACCOUNT_TYPES = ACCOUNT_PROVIDERS;

export const EMPTY_FORM = {
  name: "",
  type: "Document",
  accountType: ACCOUNT_TYPES[0],
  description: "",
  username: "",
  password: "",
  file: null,
};

export function AssetFields({ form, setForm }) {
  const [showPassword, setShowPassword] = useState(false);
  return (
    <>
      <label className={styles.label}>Asset Name</label>
      <input
        className={styles.input}
        value={form.name}
        onChange={(e) => setForm({ ...form, name: e.target.value })}
        placeholder="e.g. Property Document"
      />

      <label className={styles.label}>Asset Type</label>
      <select
        className={styles.input}
        value={form.type}
        onChange={(e) => setForm({ ...form, type: e.target.value })}
      >
        {(ASSET_TYPES.includes(form.type) ? ASSET_TYPES : [form.type, ...ASSET_TYPES]).map((t) => (
          <option key={t} value={t}>
            {t}
          </option>
        ))}
      </select>

      {!isVerifiable(form.type) && (
        <p className={styles.assetDesc} style={{ margin: "-6px 0 14px" }}>
          This type is stored as content only and cannot be verified with a provider.
        </p>
      )}

      {form.type === "Online Account" && (
        <>
          <label className={styles.label}>Account Type</label>
          <select
            className={styles.input}
            value={form.accountType}
            onChange={(e) => setForm({ ...form, accountType: e.target.value })}
          >
            {ACCOUNT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </>
      )}

      <label className={styles.label}>Description</label>
      <textarea
        className={styles.textarea}
        value={form.description}
        onChange={(e) => setForm({ ...form, description: e.target.value })}
        placeholder="Optional notes about this asset"
      />

      {!["Document","Other"].includes(form.type) && <><label className={styles.label}>Username / Account</label>
      <input
        className={styles.input}
        value={form.username}
        onChange={(e) => setForm({ ...form, username: e.target.value })}
        placeholder="Required for account verification"
        autoComplete="off"
      />

      <label className={styles.label}>Password</label>
      <div className={styles.passwordRow}>
        <input
          className={styles.input}
          type={showPassword ? "text" : "password"}
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          placeholder="Required for account verification"
          autoComplete="new-password"
        />
        <button
          type="button"
          className={styles.toggleBtn}
          onClick={() => setShowPassword((v) => !v)}
        >
          {showPassword ? "Hide" : "Show"}
        </button>
      </div></>}
    </>
  );
}

// True when the user has typed/attached something worth protecting from an accidental close.
export function isAssetFormDirty(form) {
  return Boolean(
    form.name.trim() || form.description.trim() || form.username || form.password || form.file
  );
}

export function AssetFileField({ form, setForm }) {
  const inputRef = useRef(null);
  const [fileError, setFileError] = useState("");
  const [reading, setReading] = useState(false);

  async function handleChange(e) {
    const picked = e.target.files?.[0];
    e.target.value = ""; // allow choosing the same file again
    if (!picked) return;
    setFileError("");
    setReading(true);
    try {
      const file = await readAssetFile(picked);
      setForm((prev) => ({ ...prev, file }));
    } catch (err) {
      setFileError(err.message || "Could not read this file");
    } finally {
      setReading(false);
    }
  }

  return (
    <>
      <label className={styles.label}>Upload File / Asset Information</label>
      <input ref={inputRef} type="file" style={{ display: "none" }} onChange={handleChange} />
      {form.file ? (
        <div className={styles.fileRow}>
          <span className={styles.fileName} title={form.file.name}>
            {form.file.name} <span className={styles.fileMeta}>({formatBytes(form.file.size)})</span>
          </span>
          <button type="button" className={styles.toggleBtn} onClick={() => inputRef.current?.click()}>
            Replace
          </button>
          <button
            type="button"
            className={styles.toggleBtn}
            onClick={() => setForm((prev) => ({ ...prev, file: null }))}
          >
            Remove
          </button>
        </div>
      ) : (
        <button type="button" className={styles.fileBtn} disabled={reading} onClick={() => inputRef.current?.click()}>
          {reading ? "Reading file..." : "Choose file"}
        </button>
      )}
      <p className={styles.assetDesc} style={{ margin: "0 0 14px" }}>
        Optional. Max {formatBytes(MAX_FILE_BYTES)}. The file is encrypted together with this asset.
      </p>
      {fileError && <p className={styles.error}>{fileError}</p>}
    </>
  );
}

export default function Assets() {
  const { user } = useAuth();
  const [assets, setAssets] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [showDiscard, setShowDiscard] = useState(false);

  const [selectedAsset, setSelectedAsset] = useState(null);
  const [detailMode, setDetailMode] = useState("view");
  const [detailForm, setDetailForm] = useState(EMPTY_FORM);
  const [showDetailPassword, setShowDetailPassword] = useState(false);
  const [detailError, setDetailError] = useState("");
  const [detailSaving, setDetailSaving] = useState(false);
  const secretRequest = useRef(0);
  const [secret, setSecret] = useState(null);
  const [secretLoading, setSecretLoading] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [locks, setLocks] = useState(new Map());
  const [verifying, setVerifying] = useState(false);
  const [verifyResult, setVerifyResult] = useState(null);

  useEffect(() => {
    load();
  }, [user]);

  async function load() {
    setIsLoading(true);
    setError("");
    try {
      setAssets(await getAssets(user?.id));
      // LV-18: which assets already sit in a will (shown as a lock). Failure must not block the list.
      getWills().then((w) => setLocks(buildAssetLocks(w))).catch(() => setLocks(new Map()));
    } catch (err) {
      setAssets([]);
      setError(err.message || "Could not load assets");
    } finally {
      setIsLoading(false);
    }
  }

  function openModal() {
    setForm(EMPTY_FORM);
    setError("");
    setShowDiscard(false);
    setShowModal(true);
  }

  // Backdrop click / Esc: ask first if the user already entered something.
  function requestCloseModal() {
    if (saving) return;
    if (isAssetFormDirty(form)) setShowDiscard(true);
    else setShowModal(false);
  }

  function confirmDiscard() {
    setShowDiscard(false);
    setShowModal(false);
    setForm(EMPTY_FORM);
    setError("");
  }

  const addBackdrop = useBackdropClose(requestCloseModal);

  useEffect(() => {
    if (!showModal) return undefined;
    function onKey(e) {
      if (e.key === "Escape" && !showDiscard) requestCloseModal();
    }
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  });

  async function handleAdd(e) {
    e.preventDefault();
    setError("");
    if (!form.name.trim()) {
      setError("Asset name is required");
      return;
    }
    setSaving(true);
    try {
      if (!["Document","Other"].includes(form.type) && (!form.username.trim() || !form.password)) throw new Error("Username and password are required.");
      await addAsset(user?.id, form);
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err.message || "Could not add asset");
    } finally {
      setSaving(false);
    }
  }

  async function openDetail(asset) {
    const requestId = ++secretRequest.current;
    setSelectedAsset(asset);
    setSecret(null);
    setSecretLoading(true);
    setConfirmDelete(false);
    setDetailMode("view");
    setShowDetailPassword(false);
    setDetailForm({
      name: asset.name,
      type: asset.type,
      accountType: asset.type === "Online Account" ? providerLabel(asset.accountType) || ACCOUNT_TYPES[0] : ACCOUNT_TYPES[0],
      description: asset.description || "",
      username: "",
      password: "",
      file: null,
    });
    setVerifyResult(null);
    setDetailError("");
    try {
      const revealed = await getAssetSecret(asset.id);
      if (requestId !== secretRequest.current) return;
      setSecret(revealed);
      // Prefill both fields: saving with an empty password would overwrite the stored one.
      setDetailForm(prev => ({...prev, username: revealed.username || "", password: revealed.password || "", file: assetFileFromSecret(revealed)}));
    } catch (err) {
      if (requestId === secretRequest.current) setDetailError(err.message || "Could not load asset information");
    } finally { if (requestId === secretRequest.current) setSecretLoading(false); }
  }

  function closeDetail() {
    ++secretRequest.current;
    setSelectedAsset(null);
    setSecret(null);
    setDetailForm(EMPTY_FORM);
    setShowDetailPassword(false);
    setConfirmDelete(false);
    setDetailError("");
    setDetailMode("view");
  }

  async function handleDelete() {
    setDeleting(true);
    setDetailError("");
    try {
      await deleteAsset(selectedAsset.id);
      closeDetail();
      await load();
    } catch (err) {
      setDetailError(err.message || "Cannot delete this asset. It may be linked to a Will.");
    } finally { setDeleting(false); }
  }

  async function handleVerify() {
    setVerifying(true);
    setVerifyResult(null);
    setDetailError("");
    try {
      const result = await verifyAsset(selectedAsset.id);
      setSelectedAsset((a) => ({ ...a, verificationStatus: result.status }));
      setVerifyResult({ ok: result.status === "VERIFIED", text: result.message || result.status });
      await load();
    } catch (err) {
      setDetailError(err.message || "Could not verify this asset");
    } finally {
      setVerifying(false);
    }
  }

  async function handleUpdate(e) {
    e.preventDefault();
    setDetailError("");
    if (!detailForm.name.trim()) {
      setDetailError("Asset name is required");
      return;
    }
    setDetailSaving(true);
    try {
      const updated = await updateAsset(user?.id, selectedAsset.id, detailForm);
      setSelectedAsset(updated);
      setSecret(null);
      setShowDetailPassword(false);
      try { setSecret(await getAssetSecret(updated.id)); } catch { /* Display metadata if secret unavailable */ }
      setDetailMode("view");
      await load();
    } catch (err) {
      setDetailError(err instanceof ApiError ? err.message : "Could not update asset");
    } finally {
      setDetailSaving(false);
    }
  }

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>My Digital Assets</h1>
      <p className={styles.subtitle}>Manage the assets included in your wills.</p>

      <button className={styles.addBtn} onClick={openModal}>
        + Add Asset
      </button>

      {error && !showModal && <p role="alert" className={styles.muted}>{error}</p>}
      {isLoading ? (
        <p className={styles.muted}>Loading assets...</p>
      ) : assets.length === 0 ? (
        <p className={styles.muted}>No assets yet. Add your first digital asset above.</p>
      ) : (
        <div className={styles.list}>
          {assets.map((asset) => (
            <div
              key={asset.id}
              className={styles.assetCard}
              onClick={() => openDetail(asset)}
              style={{ cursor: "pointer" }}
            >
              <div>
                <h3 className={styles.assetName}>{asset.name}</h3>
                <p className={styles.assetType}>
                  Type: {asset.type}
                  {asset.type === "Online Account" && asset.accountType ? ` · ${providerLabel(asset.accountType)}` : ""}
                </p>
                {asset.description && <p className={styles.assetDesc}>{asset.description}</p>}
                <p className={styles.assetDesc} title={verificationInfo(asset).hint}>
                  {verificationInfo(asset).label}
                  {locks.has(asset.id) ? ` · 🔒 Used in will "${locks.get(asset.id).willName}"` : ""}
                </p>
              </div>

            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className={styles.overlay} {...addBackdrop}>
          <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleAdd}>
            <h2 className={styles.modalTitle}>Add Asset</h2>

            <AssetFields form={form} setForm={setForm} />

            <AssetFileField form={form} setForm={setForm} />

            {error && <p className={styles.error}>{error}</p>}

            <div className={styles.modalActions}>
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

      {showModal && showDiscard && (
        <ConfirmDiscardDialog onConfirm={confirmDiscard} onCancel={() => setShowDiscard(false)} />
      )}

      {selectedAsset && (
        <div className={styles.overlay} onClick={closeDetail}>
          {detailMode === "view" ? (
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
              <h2 className={styles.modalTitle}>{selectedAsset.name}</h2>

              <label className={styles.label}>Asset Type</label>
              <p className={styles.assetType} style={{ marginBottom: 14 }}>{selectedAsset.type}</p>

              {selectedAsset.type === "Online Account" && (
                <>
                  <label className={styles.label}>Account Type</label>
                  <p className={styles.assetType} style={{ marginBottom: 14 }}>
                    {providerLabel(selectedAsset.accountType) || "—"}
                  </p>
                </>
              )}

              <label className={styles.label}>Description</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                {selectedAsset.description || "—"}
              </p>

              {!["Document", "Other"].includes(selectedAsset.type) ? (
                <>
                  <label className={styles.label}>Username / Account</label>
                  <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                    {secretLoading ? "Loading..." : (secret?.username || "—")}
                  </p>
                  <label className={styles.label}>Password</label>
                  <div className={styles.passwordRow} style={{ marginBottom: 14 }}>
                    <p className={styles.assetDesc} style={{ margin: 0, flex: 1 }}>
                      {secretLoading ? "Loading..." : secret?.password ? (showDetailPassword ? secret.password : "••••••••") : "—"}
                    </p>
                    {secret?.password && <button type="button" className={styles.toggleBtn}
                      onClick={() => setShowDetailPassword(v => !v)}>{showDetailPassword ? "Hide" : "Show"}</button>}
                  </div>
                </>
              ) : (
                <>
                  <label className={styles.label}>Stored content</label>
                  <p className={styles.assetDesc} style={{ marginBottom: 14 }}>{secretLoading ? "Loading..." : (secret?.content || "—")}</p>
                </>
              )}

              {secret?.fileData && (
                <>
                  <label className={styles.label}>Attached file</label>
                  <div className={styles.fileRow} style={{ marginBottom: 14 }}>
                    <span className={styles.fileName} title={secret.fileName}>
                      {secret.fileName || "file"}
                      {secret.fileSize ? <span className={styles.fileMeta}> ({formatBytes(Number(secret.fileSize))})</span> : null}
                    </span>
                    <button type="button" className={styles.toggleBtn} onClick={() => downloadAssetFile(secret)}>
                      Download
                    </button>
                  </div>
                </>
              )}

              <label className={styles.label}>Status</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>{selectedAsset.status}</p>

              <label className={styles.label}>Verification</label>
              <p className={styles.assetDesc} style={{ marginBottom: 6 }}>{verificationInfo(selectedAsset).label}</p>
              <p className={styles.assetDesc} style={{ margin: "0 0 14px" }}>{verificationInfo(selectedAsset).hint}</p>
              {isVerifiable(selectedAsset.type) && (
                <>
                  <button type="button" className={styles.toggleBtn} disabled={verifying || secretLoading} onClick={handleVerify} style={{ marginBottom: 14 }}>
                    {verifying ? "Verifying..." : "Verify with provider"}
                  </button>
                  <p className={styles.assetDesc} style={{ margin: "0 0 14px" }}>
                    Demo check: credentials are compared with the mock provider accounts, not with the real service.
                  </p>
                </>
              )}
              {verifyResult && (
                <p role="status" className={verifyResult.ok ? styles.assetType : styles.error} style={{ marginBottom: 14 }}>
                  {verifyResult.text}
                </p>
              )}
              {locks.has(selectedAsset.id) && (
                <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                  🔒 Used in will "{locks.get(selectedAsset.id).willName}".
                </p>
              )}

              {detailError && <p role="alert" className={styles.error}>{detailError}</p>}
              {confirmDelete && <p role="alert" className={styles.error}>Delete this asset permanently? Only assets not used in any Will can be deleted.</p>}
              <div className={styles.modalActions}>
                {!confirmDelete ? <button type="button" className={styles.toggleBtn} onClick={() => setConfirmDelete(true)}>Delete Asset</button> :
                  <button type="button" className={styles.toggleBtn} disabled={deleting} onClick={handleDelete}>{deleting ? "Deleting..." : "Confirm Delete"}</button>}
                <button type="button" className={styles.secondaryBtn} onClick={confirmDelete ? () => setConfirmDelete(false) : closeDetail}>
                  Close
                </button>
                <button type="button" className={styles.primaryBtn} onClick={() => setDetailMode("edit")}>
                  Edit
                </button>
              </div>
            </div>
          ) : (
            <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleUpdate}>
              <h2 className={styles.modalTitle}>Edit Asset</h2>

              <AssetFields form={detailForm} setForm={setDetailForm} />

              {detailError && <p className={styles.error}>{detailError}</p>}

              <div className={styles.modalActions}>
                <button type="button" className={styles.secondaryBtn} onClick={() => setDetailMode("view")}>
                  Cancel
                </button>
                <button type="submit" className={styles.primaryBtn} disabled={detailSaving}>
                  {detailSaving ? "Saving..." : "Save Changes"}
                </button>
              </div>
            </form>
          )}
        </div>
      )}
    </div>
  );
}
