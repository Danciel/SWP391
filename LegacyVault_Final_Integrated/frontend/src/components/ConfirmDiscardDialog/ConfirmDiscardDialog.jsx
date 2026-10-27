import { useEffect, useRef } from "react";
import styles from "./ConfirmDiscardDialog.module.css";

export default function ConfirmDiscardDialog({
  title = "Discard this asset?",
  message = "The information you entered will be lost.",
  confirmLabel = "Discard",
  cancelLabel = "Keep editing",
  onConfirm,
  onCancel,
}) {
  const cancelRef = useRef(null);

  useEffect(() => {
    cancelRef.current?.focus();
    function onKey(e) {
      if (e.key === "Escape") {
        e.stopPropagation();
        onCancel();
      }
    }
    document.addEventListener("keydown", onKey, true);
    return () => document.removeEventListener("keydown", onKey, true);
  }, [onCancel]);

  return (
    <div className={styles.overlay} role="presentation" onClick={onCancel}>
      <div
        className={styles.dialog}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="discard-title"
        aria-describedby="discard-msg"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 id="discard-title" className={styles.title}>{title}</h3>
        <p id="discard-msg" className={styles.message}>{message}</p>
        <div className={styles.actions}>
          <button type="button" ref={cancelRef} className={styles.keepBtn} onClick={onCancel}>
            {cancelLabel}
          </button>
          <button type="button" className={styles.discardBtn} onClick={onConfirm}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
