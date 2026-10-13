import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { getAssets, addAsset, updateAsset } from "../../api/mockVault";
import { ApiError } from "../../api/client";
import styles from "./Assets.module.css";

const ASSET_TYPES = ["Document", "Crypto Wallet", "Online Account", "Bank Account", "Other"];
const ACCOUNT_TYPES = [
  "Facebook",
  "Instagram",
  "TikTok",
  "X (Twitter)",
  "YouTube",
  "LinkedIn",
  "Zalo",
  "Telegram",
  "Google",
  "Other",
];

const EMPTY_FORM = {
  name: "",
  type: "Document",
  accountType: ACCOUNT_TYPES[0],
  description: "",
  username: "",
  password: "",
};

function AssetFields({ form, setForm }) {
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
        {ASSET_TYPES.map((t) => (
          <option key={t} value={t}>
            {t}
          </option>
        ))}
      </select>

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

      <label className={styles.label}>Username / Account (if any)</label>
      <input
        className={styles.input}
        value={form.username}
        onChange={(e) => setForm({ ...form, username: e.target.value })}
        placeholder="Optional"
        autoComplete="off"
      />

      <label className={styles.label}>Password (if any)</label>
      <div className={styles.passwordRow}>
        <input
          className={styles.input}
          type={showPassword ? "text" : "password"}
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          placeholder="Optional"
          autoComplete="new-password"
        />
        <button
          type="button"
          className={styles.toggleBtn}
          onClick={() => setShowPassword((v) => !v)}
        >
          {showPassword ? "Hide" : "Show"}
        </button>
      </div>
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

  const [selectedAsset, setSelectedAsset] = useState(null);
  const [detailMode, setDetailMode] = useState("view");
  const [detailForm, setDetailForm] = useState(EMPTY_FORM);
  const [showDetailPassword, setShowDetailPassword] = useState(false);
  const [detailError, setDetailError] = useState("");
  const [detailSaving, setDetailSaving] = useState(false);

  useEffect(() => {
    load();
  }, [user]);

  async function load() {
    setIsLoading(true);
    const data = await getAssets(user?.id);
    setAssets(data);
    setIsLoading(false);
  }

  function openModal() {
    setForm(EMPTY_FORM);
    setError("");
    setShowModal(true);
  }

  async function handleAdd(e) {
    e.preventDefault();
    setError("");
    if (!form.name.trim()) {
      setError("Asset name is required");
      return;
    }
    setSaving(true);
    try {
      await addAsset(user?.id, form);
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not add asset");
    } finally {
      setSaving(false);
    }
  }

  function openDetail(asset) {
    setSelectedAsset(asset);
    setDetailMode("view");
    setShowDetailPassword(false);
    setDetailForm({
      name: asset.name,
      type: asset.type,
      accountType: asset.accountType || ACCOUNT_TYPES[0],
      description: asset.description || "",
      username: asset.username || "",
      password: asset.password || "",
    });
    setDetailError("");
  }

  function closeDetail() {
    setSelectedAsset(null);
    setDetailMode("view");
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
                  {asset.type === "Online Account" && asset.accountType ? ` · ${asset.accountType}` : ""}
                </p>
                {asset.description && <p className={styles.assetDesc}>{asset.description}</p>}
              </div>
              <span className={styles.securedTag}>Secured 🔒</span>
            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className={styles.overlay} onClick={() => setShowModal(false)}>
          <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleAdd}>
            <h2 className={styles.modalTitle}>Add Asset</h2>

            <AssetFields form={form} setForm={setForm} />

            <label className={styles.label}>Upload File / Asset Information</label>
            <button type="button" className={styles.fileBtn}>
              Choose File
            </button>

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
                    {selectedAsset.accountType || "—"}
                  </p>
                </>
              )}

              <label className={styles.label}>Description</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                {selectedAsset.description || "—"}
              </p>

              <label className={styles.label}>Username / Account</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                {selectedAsset.username || "—"}
              </p>

              <label className={styles.label}>Password</label>
              <div className={styles.passwordRow} style={{ marginBottom: 14 }}>
                <p className={styles.assetDesc} style={{ margin: 0, flex: 1 }}>
                  {selectedAsset.password
                    ? showDetailPassword
                      ? selectedAsset.password
                      : "•".repeat(Math.min(selectedAsset.password.length, 12))
                    : "—"}
                </p>
                {selectedAsset.password && (
                  <button
                    type="button"
                    className={styles.toggleBtn}
                    onClick={() => setShowDetailPassword((v) => !v)}
                  >
                    {showDetailPassword ? "Hide" : "Show"}
                  </button>
                )}
              </div>

              <label className={styles.label}>Status</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>{selectedAsset.status}</p>

              <div className={styles.modalActions}>
                <button type="button" className={styles.secondaryBtn} onClick={closeDetail}>
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
