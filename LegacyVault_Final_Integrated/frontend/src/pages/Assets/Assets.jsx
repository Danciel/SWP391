import { useEffect, useState } from "react";
import {
  getAssets,
  addAsset,
  updateAsset,
  deleteAsset,
  revealAssetSecret,
  verifyAsset,
} from "../../api/assetsApi";
import { ApiError } from "../../api/client";
import styles from "./Assets.module.css";

const ASSET_TYPES = [
  "Document",
  "Crypto Wallet",
  "Online Account",
  "Cloud Storage",
  "Bank Account",
  "Other",
];
const ACCOUNT_TYPES = ["GMAIL", "GITHUB"];

const EMPTY_FORM = {
  name: "",
  type: "Document",
  accountType: "GMAIL",
  description: "",
  username: "",
  password: "",
};

function AssetFields({ form, setForm, editMode = false }) {
  const [showPassword, setShowPassword] = useState(false);
  const online = form.type === "Online Account";

  return (
    <>
      <label className={styles.label}>Asset Name</label>
      <input
        className={styles.input}
        value={form.name}
        onChange={(e) => setForm({ ...form, name: e.target.value })}
        placeholder="e.g. Personal Gmail"
      />

      <label className={styles.label}>Asset Type</label>
      <select
        className={styles.input}
        value={form.type}
        onChange={(e) => setForm({ ...form, type: e.target.value })}
      >
        {ASSET_TYPES.map((t) => (
          <option key={t} value={t}>{t}</option>
        ))}
      </select>

      {online && (
        <>
          <label className={styles.label}>Provider</label>
          <select
            className={styles.input}
            value={form.accountType}
            onChange={(e) => setForm({ ...form, accountType: e.target.value })}
          >
            {ACCOUNT_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
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

      <label className={styles.label}>
        Username / Account {editMode ? "(leave blank to keep current secret)" : ""}
      </label>
      <input
        className={styles.input}
        value={form.username}
        onChange={(e) => setForm({ ...form, username: e.target.value })}
        placeholder={online ? "Provider username/email" : "Optional"}
        autoComplete="off"
      />

      <label className={styles.label}>
        Password {editMode ? "(leave blank to keep current secret)" : ""}
      </label>
      <div className={styles.passwordRow}>
        <input
          className={styles.input}
          type={showPassword ? "text" : "password"}
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          placeholder={online ? "Provider password" : "Optional"}
          autoComplete="new-password"
        />
        <button type="button" className={styles.toggleBtn} onClick={() => setShowPassword((v) => !v)}>
          {showPassword ? "Hide" : "Show"}
        </button>
      </div>
    </>
  );
}

export default function Assets() {
  const [assets, setAssets] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  const [selectedAsset, setSelectedAsset] = useState(null);
  const [detailMode, setDetailMode] = useState("view");
  const [detailForm, setDetailForm] = useState(EMPTY_FORM);
  const [detailError, setDetailError] = useState("");
  const [detailSaving, setDetailSaving] = useState(false);
  const [secret, setSecret] = useState(null);
  const [revealing, setRevealing] = useState(false);
  const [verifying, setVerifying] = useState(false);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    setIsLoading(true);
    setPageError("");
    try {
      setAssets(await getAssets());
    } catch (err) {
      setPageError(err instanceof ApiError ? err.message : "Could not load assets");
    } finally {
      setIsLoading(false);
    }
  }

  function openModal() {
    setForm(EMPTY_FORM);
    setError("");
    setShowModal(true);
  }

  async function handleAdd(e) {
    e.preventDefault();
    setError("");
    if (!form.name.trim()) return setError("Asset name is required");
    if (form.type === "Online Account" && (!form.username.trim() || !form.password)) {
      return setError("Online Account requires username and password");
    }

    setSaving(true);
    try {
      await addAsset(form);
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err.message || "Could not add asset");
    } finally {
      setSaving(false);
    }
  }

  function openDetail(asset) {
    setSelectedAsset(asset);
    setDetailMode("view");
    setSecret(null);
    setDetailForm({
      name: asset.name,
      type: asset.type,
      accountType: asset.accountType || "GMAIL",
      description: asset.description || "",
      username: "",
      password: "",
    });
    setDetailError("");
  }

  function closeDetail() {
    setSelectedAsset(null);
    setSecret(null);
    setDetailMode("view");
  }

  async function handleUpdate(e) {
    e.preventDefault();
    setDetailError("");
    if (!detailForm.name.trim()) return setDetailError("Asset name is required");

    const replaceSecret = !!(detailForm.username.trim() || detailForm.password);
    if (replaceSecret && detailForm.type === "Online Account" && (!detailForm.username.trim() || !detailForm.password)) {
      return setDetailError("To replace an Online Account secret, enter both username and password");
    }

    setDetailSaving(true);
    try {
      const updated = await updateAsset(selectedAsset.id, detailForm, { replaceSecret });
      setSelectedAsset(updated);
      setDetailMode("view");
      setSecret(null);
      await load();
    } catch (err) {
      setDetailError(err instanceof ApiError ? err.message : err.message || "Could not update asset");
    } finally {
      setDetailSaving(false);
    }
  }

  async function handleReveal() {
    setDetailError("");
    setRevealing(true);
    try {
      const data = await revealAssetSecret(selectedAsset.id);
      setSecret(data.secretData || {});
    } catch (err) {
      setDetailError(err instanceof ApiError ? err.message : "Could not decrypt secret");
    } finally {
      setRevealing(false);
    }
  }

  async function handleVerify() {
    setDetailError("");
    setVerifying(true);
    try {
      const result = await verifyAsset(selectedAsset.id);
      setSelectedAsset((prev) => ({ ...prev, verificationStatus: result.status }));
      await load();
    } catch (err) {
      setDetailError(err instanceof ApiError ? err.message : "Could not verify asset");
    } finally {
      setVerifying(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm(`Delete ${selectedAsset.name}?`)) return;
    setDetailSaving(true);
    try {
      await deleteAsset(selectedAsset.id);
      closeDetail();
      await load();
    } catch (err) {
      setDetailError(err instanceof ApiError ? err.message : "Could not delete asset");
    } finally {
      setDetailSaving(false);
    }
  }

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>My Digital Assets</h1>
      <p className={styles.subtitle}>Real backend + encrypted secret storage.</p>

      <button className={styles.addBtn} onClick={openModal}>+ Add Asset</button>

      {pageError && <p className={styles.error}>{pageError}</p>}
      {isLoading ? (
        <p className={styles.muted}>Loading assets...</p>
      ) : assets.length === 0 ? (
        <p className={styles.muted}>No assets yet. Add your first digital asset above.</p>
      ) : (
        <div className={styles.list}>
          {assets.map((asset) => (
            <div key={asset.id} className={styles.assetCard} onClick={() => openDetail(asset)} style={{ cursor: "pointer" }}>
              <div>
                <h3 className={styles.assetName}>{asset.name}</h3>
                <p className={styles.assetType}>
                  Type: {asset.type}{asset.provider ? ` · ${asset.provider}` : ""}
                </p>
                {asset.description && <p className={styles.assetDesc}>{asset.description}</p>}
                <p className={styles.assetDesc}>Verification: {asset.verificationStatus}</p>
              </div>
              <span className={styles.securedTag}>Encrypted 🔒</span>
            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className={styles.overlay} onClick={() => setShowModal(false)}>
          <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleAdd}>
            <h2 className={styles.modalTitle}>Add Asset</h2>
            <AssetFields form={form} setForm={setForm} />
            {form.type === "Online Account" && (
              <p className={styles.assetDesc}>Demo verifier supports GMAIL and GITHUB.</p>
            )}
            {error && <p className={styles.error}>{error}</p>}
            <div className={styles.modalActions}>
              <button type="button" className={styles.secondaryBtn} onClick={() => setShowModal(false)}>Cancel</button>
              <button type="submit" className={styles.primaryBtn} disabled={saving}>{saving ? "Adding..." : "Add Asset"}</button>
            </div>
          </form>
        </div>
      )}

      {selectedAsset && (
        <div className={styles.overlay} onClick={closeDetail}>
          {detailMode === "view" ? (
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
              <h2 className={styles.modalTitle}>{selectedAsset.name}</h2>
              <label className={styles.label}>Asset Type</label>
              <p className={styles.assetType}>{selectedAsset.type}</p>
              <label className={styles.label}>Provider</label>
              <p className={styles.assetDesc}>{selectedAsset.provider || "—"}</p>
              <label className={styles.label}>Description</label>
              <p className={styles.assetDesc}>{selectedAsset.description || "—"}</p>
              <label className={styles.label}>Verification</label>
              <p className={styles.assetDesc}>{selectedAsset.verificationStatus}</p>

              <label className={styles.label}>Encrypted Secret</label>
              {secret ? (
                <div className={styles.assetDesc}>
                  {Object.entries(secret).map(([key, value]) => (
                    <p key={key}><strong>{key}:</strong> {value}</p>
                  ))}
                </div>
              ) : (
                <p className={styles.assetDesc}>Hidden. Reveal calls the authorized decrypt endpoint.</p>
              )}

              {detailError && <p className={styles.error}>{detailError}</p>}
              <div className={styles.modalActions}>
                <button className={styles.secondaryBtn} onClick={handleReveal} disabled={revealing}>
                  {revealing ? "Decrypting..." : secret ? "Reveal Again" : "Reveal Secret"}
                </button>
                {selectedAsset.provider && (
                  <button className={styles.secondaryBtn} onClick={handleVerify} disabled={verifying}>
                    {verifying ? "Verifying..." : "Verify"}
                  </button>
                )}
                <button className={styles.secondaryBtn} onClick={() => setDetailMode("edit")}>Edit</button>
                <button className={styles.secondaryBtn} onClick={handleDelete} disabled={detailSaving}>Delete</button>
                <button className={styles.primaryBtn} onClick={closeDetail}>Close</button>
              </div>
            </div>
          ) : (
            <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleUpdate}>
              <h2 className={styles.modalTitle}>Edit Asset</h2>
              <AssetFields form={detailForm} setForm={setDetailForm} editMode />
              {detailError && <p className={styles.error}>{detailError}</p>}
              <div className={styles.modalActions}>
                <button type="button" className={styles.secondaryBtn} onClick={() => setDetailMode("view")}>Cancel</button>
                <button type="submit" className={styles.primaryBtn} disabled={detailSaving}>{detailSaving ? "Saving..." : "Save Changes"}</button>
              </div>
            </form>
          )}
        </div>
      )}
    </div>
  );
}
