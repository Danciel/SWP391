import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { getAssets, addAsset, updateAsset, uploadAssetFile } from "../../api/mockVault";
import { ApiError } from "../../api/client";
import styles from "./Assets.module.css";

const ASSET_TYPES = ["Document", "Crypto Wallet", "Online Account", "Bank Account", "Other"];
const MAX_FILE_SIZE = 10 * 1024 * 1024;

const ALLOWED_FILE_TYPES = [
  "application/pdf",
  "application/msword",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "image/jpeg",
  "image/png",
  "text/plain",
];

const ALLOWED_FILE_EXTENSIONS = [
  ".pdf",
  ".doc",
  ".docx",
  ".jpg",
  ".jpeg",
  ".png",
  ".txt",
];

export default function Assets() {
  const { user } = useAuth();
  const [assets, setAssets] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState({ name: "", type: "Document", description: "" });
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [selectedFile, setSelectedFile] = useState(null);
  const [uploading, setUploading] = useState(false);

  const [selectedAsset, setSelectedAsset] = useState(null);
  const [detailMode, setDetailMode] = useState("view");
  const [detailForm, setDetailForm] = useState({ name: "", type: "Document", description: "" });
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
    setForm({ name: "", type: "Document", description: "" });
    setSelectedFile(null);
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
      const asset = await addAsset(user?.id, form);

      if (selectedFile) {
        setUploading(true);
        await uploadAssetFile(user?.id, asset.id, selectedFile);
        setUploading(false);
      }

      setShowModal(false);
      setSelectedFile(null);
      await load();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Could not add asset"
      );
    } finally {
      setSaving(false);
      setUploading(false);
    }
  }

  function handleFileChange(e) {
    const file = e.target.files?.[0] || null;

    setError("");

    if (!file) {
      setSelectedFile(null);
      return;
    }

    if (file.size > MAX_FILE_SIZE) {
      setSelectedFile(null);
      setError("File size must not exceed 10 MB");
      e.target.value = "";
      return;
    }

    const fileName = file.name.toLowerCase();
    const hasValidExtension = ALLOWED_FILE_EXTENSIONS.some((ext) =>
      fileName.endsWith(ext)
    );

    const hasValidType = ALLOWED_FILE_TYPES.includes(file.type);

    if (!hasValidExtension || !hasValidType) {
      setSelectedFile(null);
      setError(
        "Invalid file type. Allowed: PDF, DOC, DOCX, JPG, JPEG, PNG, TXT"
      );
      e.target.value = "";
      return;
    }

    setSelectedFile(file);
  }

  function openDetail(asset) {
    setSelectedAsset(asset);
    setDetailMode("view");
    setDetailForm({ name: asset.name, type: asset.type, description: asset.description || "" });
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
                <p className={styles.assetType}>Type: {asset.type}</p>
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

            <label className={styles.label}>Description</label>
            <textarea
              className={styles.textarea}
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder="Optional notes about this asset"
            />

            <label className={styles.label}>Upload File / Asset Information</label>

            <input
              type="file"
              className={styles.fileBtn}
              onChange={handleFileChange}
            />

            {selectedFile && (
              <p className={styles.assetDesc}>
                Selected: {selectedFile.name}
              </p>
            )}

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

              <label className={styles.label}>Description</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                {selectedAsset.description || "—"}
              </p>

              <label className={styles.label}>Status</label>
              <p className={styles.assetDesc} style={{ marginBottom: 14 }}>{selectedAsset.status}</p>

              <label className={styles.label}>Attached File</label>

              {selectedAsset.file ? (
                <div className={styles.assetDesc} style={{ marginBottom: 14 }}>
                  <p>
                    <strong>Name:</strong> {selectedAsset.file.name}
                  </p>

                  <p>
                    <strong>Type:</strong> {selectedAsset.file.type}
                  </p>

                  <p>
                    <strong>Size:</strong>{" "}
                    {(selectedAsset.file.size / 1024).toFixed(1)} KB
                  </p>

                  <p>
                    <strong>Secured:</strong>{" "}
                    {selectedAsset.file.secured ? "Yes" : "No"}
                  </p>
                </div>
              ) : (
                <p className={styles.assetDesc} style={{ marginBottom: 14 }}>
                  No file attached.
                </p>
              )}

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

              <label className={styles.label}>Asset Name</label>
              <input
                className={styles.input}
                value={detailForm.name}
                onChange={(e) => setDetailForm({ ...detailForm, name: e.target.value })}
                placeholder="e.g. Property Document"
              />

              <label className={styles.label}>Asset Type</label>
              <select
                className={styles.input}
                value={detailForm.type}
                onChange={(e) => setDetailForm({ ...detailForm, type: e.target.value })}
              >
                {ASSET_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>

              <label className={styles.label}>Description</label>
              <textarea
                className={styles.textarea}
                value={detailForm.description}
                onChange={(e) => setDetailForm({ ...detailForm, description: e.target.value })}
                placeholder="Optional notes about this asset"
              />

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
