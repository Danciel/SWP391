import { useMemo, useState } from "react";
import DeathCertificate from "./DeathCertificate";
import {
  EMPTY_CERTIFICATE,
  buildCertificateSvg,
  svgToPngBlob,
  validateCertificate,
} from "../../utils/deathCertificate";
import styles from "./DeathCertificateModal.module.css";

const FIELDS = [
  ["deceasedName", "Full name of the deceased *", "text"],
  ["gender", "Sex", "select"],
  ["dateOfBirth", "Date of birth", "date"],
  ["nationality", "Nationality", "text"],
  ["idNumber", "ID number", "text"],
  ["lastResidence", "Last residence", "text"],
  ["dateOfDeath", "Date of death *", "date"],
  ["timeOfDeath", "Time of death (e.g. 03:20)", "text"],
  ["placeOfDeath", "Place of death *", "text"],
  ["causeOfDeath", "Cause of death *", "text"],
  ["issuingAuthority", "Issuing authority *", "text"],
  ["registrationNumber", "Registration no. *", "text"],
  ["bookNumber", "Book no.", "text"],
  ["registrationDate", "Registration date *", "date"],
  ["registrarName", "Registrar / signer *", "text"],
];

// Executor: fill the form -> certificate with seal -> submitted to Legal Verify.
// Or upload a scanned file (PDF / JPG / PNG) instead.
export default function DeathCertificateModal({ ownerName, busy, onSubmit, onClose }) {
  const [mode, setMode] = useState("form");
  const [data, setData] = useState({ ...EMPTY_CERTIFICATE, deceasedName: ownerName || "" });
  const [file, setFile] = useState(null);
  const [error, setError] = useState("");
  const [working, setWorking] = useState(false);

  const preview = useMemo(() => data, [data]);
  const set = (key) => (e) => setData((prev) => ({ ...prev, [key]: e.target.value }));

  async function submitForm() {
    const problem = validateCertificate(data);
    if (problem) return setError(problem);

    setError("");
    setWorking(true);
    try {
      const blob = await svgToPngBlob(buildCertificateSvg(data));
      const png = new File([blob], `death-certificate-${Date.now()}.png`, { type: "image/png" });
      await onSubmit({ file: png, certificateData: data });
    } catch (err) {
      setError(err.message || "Could not submit the certificate.");
    } finally {
      setWorking(false);
    }
  }

  async function submitUpload() {
    if (!file) return setError("Choose a PDF, JPG or PNG file first.");
    setError("");
    try {
      await onSubmit({ file, certificateData: null });
    } catch (err) {
      setError(err.message || "Could not submit the file.");
    }
  }

  const disabled = busy || working;

  return (
    <div className={styles.overlay} onClick={() => !disabled && onClose()}>
      <div className={styles.modal} role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <div className={styles.head}>
          <h2 className={styles.title}>Submit death certificate</h2>
          <button className={styles.close} onClick={onClose} disabled={disabled} aria-label="Close">×</button>
        </div>

        <div className={styles.tabs}>
          <button className={mode === "form" ? styles.tabOn : styles.tab} onClick={() => setMode("form")}>
            Fill the certificate form
          </button>
          <button className={mode === "upload" ? styles.tabOn : styles.tab} onClick={() => setMode("upload")}>
            Upload a scanned file
          </button>
        </div>

        {mode === "form" ? (
          <div className={styles.split}>
            <div className={styles.form}>
              {FIELDS.map(([key, label, type]) => (
                <label key={key} className={styles.field}>
                  <span>{label}</span>
                  {type === "select" ? (
                    <select value={data[key]} onChange={set(key)}>
                      <option>Nam</option>
                      <option>Nữ</option>
                    </select>
                  ) : (
                    <input type={type} value={data[key]} onChange={set(key)} maxLength={160} />
                  )}
                </label>
              ))}
            </div>
            <div className={styles.preview}>
              <p className={styles.previewLabel}>Live preview (with seal)</p>
              <DeathCertificate data={preview} maxWidth={360} />
            </div>
          </div>
        ) : (
          <label className={styles.upload}>
            <input
              type="file"
              accept=".pdf,.jpg,.jpeg,.png"
              onChange={(e) => setFile(e.target.files?.[0] || null)}
            />
            <span>{file ? file.name : "Choose a PDF, JPG or PNG (max 5 MB)"}</span>
          </label>
        )}

        {error && <p className={styles.error} role="alert">{error}</p>}

        <div className={styles.actions}>
          <button className={styles.secondary} onClick={onClose} disabled={disabled}>Cancel</button>
          <button
            className={styles.primary}
            disabled={disabled}
            onClick={mode === "form" ? submitForm : submitUpload}
          >
            {disabled ? "Submitting..." : "Submit to Legal Verify"}
          </button>
        </div>
      </div>
    </div>
  );
}
