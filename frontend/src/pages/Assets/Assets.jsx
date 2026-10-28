
import SecurityPinModal from "../../components/SecurityPinModal/SecurityPinModal";
import { useEffect, useState, useRef } from "react";
import { useAuth } from "../../context/AuthContext";
import {
  getAssets,
  addAsset,
  updateAsset,
  getAssetSecret,
  deleteAsset,
  getWills,
  checkDocumentIntegrity,
  downloadDocument,
} from "../../api/vaultApi";
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
<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
=======
import ConfirmDiscardDialog from "../../components/ConfirmDiscardDialog/ConfirmDiscardDialog";
import {
  MAX_FILE_BYTES,
  formatBytes,
  readAssetFile,
  downloadAssetFile,
} from "../../utils/assetFile";
import { useBackdropClose } from "../../utils/useBackdropClose";
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
import styles from "./Assets.module.css";

// Re-exported so CreateWill keeps working with the same import.
export const ASSET_TYPES = ENABLED_ASSET_TYPES;
export const ACCOUNT_TYPES = ACCOUNT_PROVIDERS;

export const EMPTY_FORM = {
  name: "",
  type: "Document",
  accountType: "GMAIL",
  description: "",
  expectedIdentifier: "",
  handoverPassword: "",
  handoverConsent: false,
  fileId: "",
  file: null,
  username: "",
  password: "",
};

// ==========================================
// ASSET FORM
// ==========================================
export function AssetFields({ form, setForm, editing = false }) {
  const set = (patch) =>
    setForm((prev) => ({ ...prev, ...patch }));

  const isAccount = form.type === "Online Account";
  const isCloud = form.type === "Cloud Storage";

  return (
    <>
      <label className={styles.label}>Asset Name</label>
      <input
        className={styles.input}
        required
        maxLength={150}
        value={form.name || ""}
        onChange={(e) => set({ name: e.target.value })}
        placeholder="Enter asset name"
      />

      <label className={styles.label}>Asset Type</label>
      <select
        className={styles.input}
        value={form.type}
        disabled={editing}
        onChange={(e) =>
          set({
            type: e.target.value,
            accountType:
              e.target.value === "Cloud Storage"
                ? "GOOGLE_DRIVE"
                : "GMAIL",
            expectedIdentifier: "",
            handoverPassword: "",
            handoverConsent: false,
            fileId: "",
            file: null,
          })
        }
      >
        {(
          ASSET_TYPES.includes(form.type)
            ? ASSET_TYPES
            : [form.type, ...ASSET_TYPES]
        ).map((t) => (
          <option key={t} value={t}>
            {t}
          </option>
        ))}
      </select>

      {(isAccount || isCloud) && (
        <>
          <label className={styles.label}>Provider</label>
          <select
            className={styles.input}
            value={
              form.accountType ||
              (isCloud ? "GOOGLE_DRIVE" : "GMAIL")
            }
            disabled={editing || isCloud}
            onChange={(e) =>
              set({
                accountType: e.target.value,
                expectedIdentifier: "",
                handoverPassword: "",
                handoverConsent: false,
              })
            }
          >
            {(isCloud
              ? ["GOOGLE_DRIVE"]
              : ACCOUNT_TYPES
            ).map((code) => (
              <option key={code} value={code}>
                {providerLabel(code)}
              </option>
            ))}
          </select>
        </>
      )}

      {isAccount && !editing && (
        <>
          <label className={styles.label}>
            {form.accountType === "GITHUB"
              ? "GitHub Username or Verified Email"
              : "Gmail Address"}
          </label>
          <input
            className={styles.input}
            required
            type={
              form.accountType === "GITHUB"
                ? "text"
                : "email"
            }
            maxLength={254}
            autoComplete="off"
            value={form.expectedIdentifier || ""}
            onChange={(e) =>
              set({ expectedIdentifier: e.target.value })
            }
            placeholder={
              form.accountType === "GITHUB"
                ? "GitHub username or verified email"
                : "example@gmail.com"
            }
          />
        </>
      )}

      {isAccount && (
        <>
          <label className={styles.label}>
            {editing
              ? "New Handover Password (Optional)"
              : "Password for Handover (Optional)"}
          </label>
          <input
            className={styles.input}
            type="password"
            autoComplete="new-password"
            maxLength={1024}
            value={form.handoverPassword || ""}
            onChange={(e) =>
              set({ handoverPassword: e.target.value })
            }
            placeholder={
              editing
                ? "Leave blank to keep current password"
                : "Enter password to encrypt"
            }
          />

          {!!form.handoverPassword && (
            <label className={styles.label}>
              <input
                type="checkbox"
                required
                checked={!!form.handoverConsent}
                onChange={(e) =>
                  set({
                    handoverConsent: e.target.checked,
                  })
                }
              />
              {" "}I authorize secure storage of this password.
            </label>
          )}
        </>
      )}

      {isCloud && !editing && (
        <>
          <label className={styles.label}>
            Google Drive File ID
          </label>
          <input
            className={styles.input}
            required
            maxLength={512}
            value={form.fileId || ""}
            onChange={(e) =>
              set({ fileId: e.target.value })
            }
            placeholder="Google Drive File ID"
          />
        </>
      )}

      <label className={styles.label}>Description</label>
      <textarea
        className={styles.textarea}
        value={form.description || ""}
        onChange={(e) =>
          set({ description: e.target.value })
        }
        placeholder="Optional notes about this asset"
      />
    </>
  );
}

<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
=======
export function isAssetFormDirty(form) {
  return Boolean(
    (form.name || "").trim() ||
    (form.description || "").trim() ||
    form.expectedIdentifier ||
    form.handoverPassword ||
    form.fileId ||
    form.file
  );
}

// ==========================================
// DOCUMENT FILE FIELD
// ==========================================
export function AssetFileField({ form, setForm }) {
  const inputRef = useRef(null);
  const [fileError, setFileError] = useState("");
  const [reading, setReading] = useState(false);

  async function handleChange(e) {
    const picked = e.target.files?.[0];
    e.target.value = "";

    if (!picked) return;

    setFileError("");
    setReading(true);

    try {
      const file = await readAssetFile(picked);

      setForm((prev) => ({
        ...prev,
        file,
      }));
    } catch (err) {
      setFileError(
        err.message || "Could not read this file"
      );
    } finally {
      setReading(false);
    }
  }

  if (form.type !== "Document") return null;

  return (
    <>
      <label className={styles.label}>
        Upload Document (PDF / JPG / PNG)
      </label>

      <input
        ref={inputRef}
        type="file"
        style={{ display: "none" }}
        onChange={handleChange}
      />

      {form.file ? (
        <div className={styles.fileRow}>
          <span
            className={styles.fileName}
            title={form.file.name}
          >
            {form.file.name}{" "}
            <span className={styles.fileMeta}>
              ({formatBytes(form.file.size)})
            </span>
          </span>

          <button
            type="button"
            className={styles.toggleBtn}
            onClick={() => inputRef.current?.click()}
          >
            Replace
          </button>

          <button
            type="button"
            className={styles.toggleBtn}
            onClick={() =>
              setForm((prev) => ({
                ...prev,
                file: null,
              }))
            }
          >
            Remove
          </button>
        </div>
      ) : (
        <button
          type="button"
          className={styles.fileBtn}
          disabled={reading}
          onClick={() => inputRef.current?.click()}
        >
          {reading ? "Reading file..." : "Choose file"}
        </button>
      )}

      <p
        className={styles.assetDesc}
        style={{ margin: "0 0 14px" }}
      >
        Maximum file size: {formatBytes(MAX_FILE_BYTES)}.
      </p>

      {fileError && (
        <p className={styles.error}>{fileError}</p>
      )}
    </>
  );
}

// ==========================================
// MAIN ASSETS PAGE
// ==========================================

>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
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

  const secretRequest = useRef(0);
  const [secret, setSecret] = useState(null);
  const [secretLoading, setSecretLoading] = useState(false);

  const [showPinModal, setShowPinModal] = useState(false);

  // "show": nhap PIN de xem mat khau
  // "edit": nhap PIN de sua Asset
  const [pinAction, setPinAction] = useState(null);
  const [editPinGrant, setEditPinGrant] = useState(null);

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
<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
      // LV-18: which assets already sit in a will (shown as a lock). Failure must not block the list.
      getWills().then((w) => setLocks(buildAssetLocks(w))).catch(() => setLocks(new Map()));
=======

      getWills()
        .then((w) => setLocks(buildAssetLocks(w)))
        .catch(() => setLocks(new Map()));
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
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
    setShowModal(true);
  }

<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
=======
  function requestCloseModal() {
    if (saving) return;

    if (isAssetFormDirty(form)) {
      setShowDiscard(true);
    } else {
      setShowModal(false);
    }
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
      if (e.key === "Escape" && !showDiscard) {
        requestCloseModal();
      }
    }

    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  });

>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
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
      setError(err.message || "Could not add asset");
    } finally {
      setSaving(false);
    }
  }

  // ==========================================
  // OPEN ASSET DETAIL
  // ==========================================
  function openDetail(asset) {
    ++secretRequest.current;

    setSelectedAsset(asset);
    setSecret(null);
    setSecretLoading(false);
    setShowDetailPassword(false);

    setShowPinModal(false);
    setPinAction(null);
    setEditPinGrant(null);

    setConfirmDelete(false);
    setDetailMode("view");

    setDetailForm({
      name: asset.name,
      type: asset.type,
      accountType:
        asset.accountType ||
        (asset.type === "Document" ? "LOCAL_DOCUMENT" : "GMAIL"),
      description: asset.description || "",
<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
      username: "",
      password: "",
=======
      expectedIdentifier: "",
      handoverPassword: "",
      handoverConsent: false,
      fileId: "",
      file: null,
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
    });

    setVerifyResult(null);
    setDetailError("");
<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
    try {
      const revealed = await getAssetSecret(asset.id);
      if (requestId !== secretRequest.current) return;
      setSecret(revealed);
      // Prefill both fields: saving with an empty password would overwrite the stored one.
      setDetailForm(prev => ({...prev, username: revealed.username || "", password: revealed.password || ""}));
    } catch (err) {
      if (requestId === secretRequest.current) setDetailError(err.message || "Could not load asset information");
    } finally { if (requestId === secretRequest.current) setSecretLoading(false); }
=======
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
  }

  // ==========================================
  // YEU CAU PIN CHO SHOW PASSWORD
  // ==========================================
  function handlePasswordToggle() {
    if (showDetailPassword) {
      ++secretRequest.current;

      setShowDetailPassword(false);
      setSecret(null);
      return;
    }

    setDetailError("");
    setPinAction("show");
    setShowPinModal(true);
  }

  // ==========================================
  // YEU CAU PIN CHO EDIT ASSET
  // ==========================================
  function requestEditWithPin() {
    // Moi lan bam Edit deu phai nhap PIN
    ++secretRequest.current;

    setSecret(null);
    setShowDetailPassword(false);
    setEditPinGrant(null);

    setDetailError("");
    setPinAction("edit");
    setShowPinModal(true);
  }

  // ==========================================
  // XU LY SAU KHI PIN DUOC XAC MINH
  // ==========================================
  async function handleAssetPinVerified(grant) {
    if (!selectedAsset) return;

    // PIN dung de DELETE ASSET
    if (pinAction === "delete") {
      await handleDeleteWithGrant(grant);
      return;
    }

    // PIN dung de EDIT
    if (pinAction === "edit") {
      ++secretRequest.current;

      setSecret(null);
      setShowDetailPassword(false);

      // Luu Grant tam thoi trong React state
      setEditPinGrant(grant);

      // Chi bay gio moi mo form Edit
      setDetailMode("edit");
      setShowPinModal(false);
      setPinAction(null);
      setDetailError("");
      return;
    }

    // PIN dung de SHOW PASSWORD
    const assetId = selectedAsset.id;
    const requestId = ++secretRequest.current;

    setSecretLoading(true);
    setDetailError("");

    try {
      const revealed = await getAssetSecret(assetId, grant);

      if (requestId !== secretRequest.current) {
        return;
      }

      setSecret(revealed);
      setShowDetailPassword(true);
      setShowPinModal(false);
      setPinAction(null);
    } catch (err) {
      if (requestId === secretRequest.current) {
        setSecret(null);
        setShowDetailPassword(false);
      }

      throw err;
    } finally {
      if (requestId === secretRequest.current) {
        setSecretLoading(false);
      }
    }
  }

  // ==========================================
  // CLOSE ASSET DETAIL
  // ==========================================
  function closeDetail() {
    ++secretRequest.current;

    setSelectedAsset(null);
    setSecret(null);
    setDetailForm(EMPTY_FORM);

    setShowDetailPassword(false);
    setShowPinModal(false);
    setPinAction(null);
    setEditPinGrant(null);
    setSecretLoading(false);

    setConfirmDelete(false);
    setDetailError("");
    setDetailMode("view");
  }

  // ==========================================
  // DELETE ASSET
  // ==========================================
  // Buoc 1: bam "Delete Asset" -> hien popup hoi chac chan
  function requestDelete() {
    setDetailError("");

    if (locks.has(selectedAsset.id)) {
      setDetailError(
        `This asset is used in will "${locks.get(selectedAsset.id).willName}". ` +
        "Remove it from the will before deleting."
      );
      return;
    }

    setConfirmDelete(true);
  }

  // Buoc 2: bam "Yes, continue" -> nhap PIN
  function continueDeleteToPin() {
    setConfirmDelete(false);
    setPinAction("delete");
    setShowPinModal(true);
  }

  // Buoc 3: PIN dung -> Backend cap Grant -> xoa Asset
  async function handleDeleteWithGrant(grant) {
    setDeleting(true);
    setDetailError("");

    try {
      await deleteAsset(selectedAsset.id, grant);
      closeDetail();
      await load();
    } catch (err) {
      // Dong popup PIN va hien loi trong chi tiet Asset
      setShowPinModal(false);
      setPinAction(null);
      setDetailError(
        err.message ||
        "Cannot delete this asset. It may be linked to a Will."
      );
    } finally {
      setDeleting(false);
    }
  }

  // ==========================================
  // VERIFY ASSET
  // ==========================================
  async function handleVerify() {
    setVerifying(true);
    setVerifyResult(null);
    setDetailError("");

    try {
      const result = await verifyAsset(selectedAsset.id);

      setSelectedAsset((a) => ({
        ...a,
        verificationStatus: result.status,
      }));

      setVerifyResult({
        ok: result.status === "VERIFIED",
        text: result.message || result.status,
      });

      await load();
    } catch (err) {
      setDetailError(
        err.message || "Could not verify this asset"
      );
    } finally {
      setVerifying(false);
    }
  }

  // ==========================================
  // UPDATE ASSET
  // ==========================================
  async function handleUpdate(e) {
    e.preventDefault();
    setDetailError("");

    if (!detailForm.name.trim()) {
      setDetailError("Asset name is required");
      return;
    }

    if (!editPinGrant) {
      setDetailError("Please verify your PIN before saving.");
      return;
    }

    setDetailSaving(true);

    try {
      const updated = await updateAsset(
        user?.id,
        selectedAsset.id,
        detailForm,
        editPinGrant
      );

      ++secretRequest.current;

      setSelectedAsset(updated);
      setSecret(null);
      setShowDetailPassword(false);
      setShowPinModal(false);
      setEditPinGrant(null);
      setSecretLoading(false);

      setDetailForm((prev) => ({
        ...prev,
        handoverPassword: "",
        handoverConsent: false,
      }));

      setDetailMode("view");
      await load();
    } catch (err) {
      if (err?.status === 423) {
        setEditPinGrant(null);
        setDetailError(
          "PIN verification expired. Please re-verify PIN to save."
        );
      } else {
        setDetailError(
          err instanceof ApiError
            ? err.message
            : err.message || "Could not update asset"
        );
      }
    } finally {
      setDetailSaving(false);
    }
  }

  function cancelEdit() {
    setEditPinGrant(null);
    setDetailError("");
    setDetailMode("view");
  }

  // ==========================================
  // RENDER
  // ==========================================
  return (
    <div className={styles.page}>
      <h1 className={styles.title}>My Digital Assets</h1>
      <p className={styles.subtitle}>
        Manage the assets included in your wills.
      </p>

      <button className={styles.addBtn} onClick={openModal}>
        + Add Asset
      </button>

      {error && !showModal && (
        <p role="alert" className={styles.muted}>
          {error}
        </p>
      )}

      {/* ASSET LIST */}
      {isLoading ? (
        <p className={styles.muted}>Loading assets...</p>
      ) : assets.length === 0 ? (
        <p className={styles.muted}>
          No assets yet. Add your first digital asset above.
        </p>
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
                <h3 className={styles.assetName}>
                  {asset.name}
                </h3>

                <p className={styles.assetType}>
                  Type: {asset.type}
                  {asset.type === "Online Account" && asset.accountType
                    ? ` · ${providerLabel(asset.accountType)}`
                    : ""}
                </p>

                {asset.description && (
                  <p className={styles.assetDesc}>
                    {asset.description}
                  </p>
                )}

                <p
                  className={styles.assetDesc}
                  title={verificationInfo(asset).hint}
                >
                  {verificationInfo(asset).label}

                  {locks.has(asset.id)
                    ? ` · 🔒 Used in will "${locks.get(asset.id).willName}"`
                    : ""}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* ADD ASSET */}
      {showModal && (
<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
        <div className={styles.overlay} onClick={() => setShowModal(false)}>
          <form className={styles.modal} onClick={(e) => e.stopPropagation()} onSubmit={handleAdd}>
            <h2 className={styles.modalTitle}>Add Asset</h2>

            <AssetFields form={form} setForm={setForm} />

            <label className={styles.label}>Upload File / Asset Information</label>
            <button type="button" className={styles.fileBtn} disabled title="File uploads are not supported by the backend">
              File upload not available yet
            </button>
=======
        <div className={styles.overlay} {...addBackdrop}>
          <form
            className={styles.modal}
            onClick={(e) => e.stopPropagation()}
            onSubmit={handleAdd}
          >
            <h2 className={styles.modalTitle}>Add Asset</h2>

            <AssetFields form={form} setForm={setForm} />
            <AssetFileField form={form} setForm={setForm} />
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx

            {error && (
              <p className={styles.error}>{error}</p>
            )}

            <div className={styles.modalActions}>
              <button
                type="button"
                className={styles.secondaryBtn}
                onClick={() => setShowModal(false)}
              >
                Cancel
              </button>

              <button
                type="submit"
                className={styles.primaryBtn}
                disabled={saving}
              >
                {saving ? "Adding..." : "Add Asset"}
              </button>
            </div>
          </form>
        </div>
      )}

<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
=======
      {showModal && showDiscard && (
        <ConfirmDiscardDialog
          onConfirm={confirmDiscard}
          onCancel={() => setShowDiscard(false)}
        />
      )}

      {/* ASSET DETAIL */}
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
      {selectedAsset && (
        <div className={styles.overlay} onClick={closeDetail}>
          {detailMode === "view" ? (
            <div
              className={styles.modal}
              onClick={(e) => e.stopPropagation()}
            >
              <h2 className={styles.modalTitle}>
                {selectedAsset.name}
              </h2>

              <label className={styles.label}>Asset Type</label>
              <p
                className={styles.assetType}
                style={{ marginBottom: 14 }}
              >
                {selectedAsset.type}
              </p>

              {selectedAsset.type === "Online Account" && (
                <>
                  <label className={styles.label}>
                    Account Type
                  </label>
                  <p
                    className={styles.assetType}
                    style={{ marginBottom: 14 }}
                  >
                    {providerLabel(selectedAsset.accountType) || "—"}
                  </p>
                </>
              )}

              <label className={styles.label}>Description</label>
              <p
                className={styles.assetDesc}
                style={{ marginBottom: 14 }}
              >
                {selectedAsset.description || "—"}
              </p>

              {!["Document", "Other"].includes(
                selectedAsset.type
              ) ? (
                <>
                  <label className={styles.label}>
                    Username / Account
                  </label>
                  <p
                    className={styles.assetDesc}
                    style={{ marginBottom: 14 }}
                  >
                    {secretLoading
                      ? "Unlocking..."
                      : secret?.verifiedAccountIdentifier ||
                        secret?.handoverUsername ||
                        secret?.username ||
                        "Protected"}
                  </p>

                  <label className={styles.label}>
                    Password
                  </label>

                  <div
                    className={styles.passwordRow}
                    style={{ marginBottom: 14 }}
                  >
                    <p
                      className={styles.assetDesc}
                      style={{ margin: 0, flex: 1 }}
                    >
                      {secretLoading
                        ? "Unlocking..."
                        : showDetailPassword
                          ? secret?.handoverPassword ||
                            secret?.password ||
                            "No password saved"
                          : "••••••••"}
                    </p>

                    <button
                      type="button"
                      className={styles.toggleBtn}
                      disabled={secretLoading}
                      onClick={handlePasswordToggle}
                    >
                      {showDetailPassword ? "Hide" : "Show"}
                    </button>
                  </div>
                </>
              ) : (
                <>
                  <label className={styles.label}>
                    Stored content
                  </label>
                  <p
                    className={styles.assetDesc}
                    style={{ marginBottom: 14 }}
                  >
                    {secretLoading
                      ? "Unlocking..."
                      : showDetailPassword
                        ? secret?.content || "—"
                        : "Protected"}
                  </p>

                  <button
                    type="button"
                    className={styles.toggleBtn}
                    disabled={secretLoading}
                    onClick={handlePasswordToggle}
                    style={{ marginBottom: 14 }}
                  >
                    {showDetailPassword
                      ? "Lock content"
                      : "Unlock with PIN"}
                  </button>
                </>
              )}

<<<<<<< Updated upstream:frontend/src/pages/Assets/Assets.jsx
=======
              {/* ATTACHED FILE */}
              {secret?.fileData && (
                <>
                  <label className={styles.label}>
                    Attached file
                  </label>
                  <div
                    className={styles.fileRow}
                    style={{ marginBottom: 14 }}
                  >
                    <span
                      className={styles.fileName}
                      title={secret.fileName}
                    >
                      {secret.fileName || "file"}

                      {secret.fileSize ? (
                        <span className={styles.fileMeta}>
                          {" "}({formatBytes(Number(secret.fileSize))})
                        </span>
                      ) : null}
                    </span>

                    <button
                      type="button"
                      className={styles.toggleBtn}
                      onClick={() => downloadAssetFile(secret)}
                    >
                      Download
                    </button>
                  </div>
                </>
              )}

>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/Assets/Assets.jsx
              <label className={styles.label}>Status</label>
              <p
                className={styles.assetDesc}
                style={{ marginBottom: 14 }}
              >
                {selectedAsset.status}
              </p>

              <label className={styles.label}>
                Verification
              </label>
              <p
                className={styles.assetDesc}
                style={{ marginBottom: 6 }}
              >
                {verificationInfo(selectedAsset).label}
              </p>
              <p
                className={styles.assetDesc}
                style={{ margin: "0 0 14px" }}
              >
                {verificationInfo(selectedAsset).hint}
              </p>

              {isVerifiable(selectedAsset.type) &&
                ![
                  "GMAIL",
                  "GITHUB",
                  "GOOGLE_DRIVE",
                  "LOCAL_DOCUMENT",
                ].includes(
                  String(selectedAsset.accountType).toUpperCase()
                ) && (
                  <>
                    <button
                      type="button"
                      className={styles.toggleBtn}
                      disabled={verifying || secretLoading}
                      onClick={handleVerify}
                      style={{ marginBottom: 14 }}
                    >
                      {verifying
                        ? "Verifying..."
                        : "Verify with provider"}
                    </button>
                    <p
                      className={styles.assetDesc}
                      style={{ margin: "0 0 14px" }}
                    >
                      Demo check: credentials are compared with
                      the mock provider accounts, not with the
                      real service.
                    </p>
                  </>
                )}

              {/* DOCUMENT INTEGRITY & DOWNLOAD */}
              {selectedAsset.assetType === "DOCUMENT" &&
                String(selectedAsset.accountType).toUpperCase() ===
                  "LOCAL_DOCUMENT" && (
                  <div
                    className={styles.fileRow}
                    style={{ marginBottom: 14 }}
                  >
                    <button
                      type="button"
                      className={styles.toggleBtn}
                      onClick={async () => {
                        try {
                          const result =
                            await checkDocumentIntegrity(
                              selectedAsset.id
                            );
                          setVerifyResult({
                            ok: !!result.integrityVerified,
                            text: result.integrityVerified
                              ? "SHA-256 integrity verified"
                              : "Integrity check failed",
                          });
                        } catch (err) {
                          setDetailError(
                            err.message || "Integrity check failed"
                          );
                        }
                      }}
                    >
                      Check SHA-256
                    </button>

                    <button
                      type="button"
                      className={styles.toggleBtn}
                      onClick={async () => {
                        try {
                          await downloadDocument(selectedAsset.id);
                        } catch (err) {
                          setDetailError(
                            err.message || "Download failed"
                          );
                        }
                      }}
                    >
                      Download Document
                    </button>
                  </div>
                )}

              {verifyResult && (
                <p
                  role="status"
                  className={
                    verifyResult.ok
                      ? styles.assetType
                      : styles.error
                  }
                  style={{ marginBottom: 14 }}
                >
                  {verifyResult.text}
                </p>
              )}

              {locks.has(selectedAsset.id) && (
                <p
                  className={styles.assetDesc}
                  style={{ marginBottom: 14 }}
                >
                  🔒 Used in will "
                  {locks.get(selectedAsset.id).willName}".
                </p>
              )}

              {detailError && (
                <p role="alert" className={styles.error}>
                  {detailError}
                </p>
              )}

              <div className={styles.modalActions}>
                <button
                  type="button"
                  className={styles.toggleBtn}
                  disabled={deleting}
                  onClick={requestDelete}
                >
                  Delete Asset
                </button>

                <button
                  type="button"
                  className={styles.secondaryBtn}
                  onClick={closeDetail}
                >
                  Close
                </button>

                {/* EDIT CAN NHAP PIN */}
                <button
                  type="button"
                  className={styles.primaryBtn}
                  onClick={requestEditWithPin}
                >
                  Edit
                </button>
              </div>
            </div>
          ) : (
            /* EDIT ASSET */
            <form
              className={styles.modal}
              onClick={(e) => e.stopPropagation()}
              onSubmit={handleUpdate}
            >
              <h2 className={styles.modalTitle}>
                Edit Asset
              </h2>

              <AssetFields
                form={detailForm}
                setForm={setDetailForm}
                editing
              />

              {detailError && (
                <p className={styles.error}>
                  {detailError}
                </p>
              )}

              <div className={styles.modalActions}>
                <button
                  type="button"
                  className={styles.secondaryBtn}
                  onClick={cancelEdit}
                >
                  Cancel
                </button>

                {/* NHAP LAI PIN NEU GRANT HET HAN */}
                {!editPinGrant && (
                  <button
                    type="button"
                    className={styles.toggleBtn}
                    onClick={() => {
                      setPinAction("edit");
                      setShowPinModal(true);
                    }}
                  >
                    Re-verify PIN
                  </button>
                )}

                <button
                  type="submit"
                  className={styles.primaryBtn}
                  disabled={detailSaving || !editPinGrant}
                >
                  {detailSaving
                    ? "Saving..."
                    : "Save Changes"}
                </button>
              </div>
            </form>
          )}
        </div>
      )}

      {/* DELETE ASSET: popup hoi chac chan truoc khi nhap PIN */}
      {confirmDelete && selectedAsset && (
        <div
          className={styles.overlay}
          style={{ zIndex: 60 }}
          onClick={() => setConfirmDelete(false)}
        >
          <div
            className={styles.modal}
            role="dialog"
            aria-modal="true"
            aria-label="Delete asset"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className={styles.modalTitle}>Delete this asset?</h2>
            <p className={styles.assetDesc} style={{ marginBottom: 18 }}>
              Are you sure you want to permanently delete{" "}
              <strong>{selectedAsset.name}</strong>? This cannot be undone.
              You will need to enter your Security PIN to confirm.
            </p>
            <div className={styles.modalActions}>
              <button
                type="button"
                className={styles.secondaryBtn}
                onClick={() => setConfirmDelete(false)}
              >
                Cancel
              </button>
              <button
                type="button"
                className={styles.primaryBtn}
                style={{ background: "var(--bad)" }}
                onClick={continueDeleteToPin}
              >
                Yes, continue
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ONE SECURITY PIN FOR SHOW, EDIT AND DELETE */}
      {showPinModal && selectedAsset && (
        <SecurityPinModal
          scope={pinAction === "delete" ? "ASSET_DELETE" : "ASSET_SECRET"}
          resourceId={selectedAsset.id}
          onVerified={handleAssetPinVerified}
          onClose={() => {
            setShowPinModal(false);
            setPinAction(null);
          }}
        />
      )}
    </div>
  );
}
