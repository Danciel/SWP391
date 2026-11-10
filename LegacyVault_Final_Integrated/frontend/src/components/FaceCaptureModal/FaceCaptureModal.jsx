import { useEffect, useRef, useState } from "react";
import styles from "./FaceCaptureModal.module.css";

// Opens the camera, takes one JPEG frame and hands it to onCapture(blob).
// onCapture may throw: the message is shown and the person can try again.
export default function FaceCaptureModal({ title = "Face verification", hint, onCapture, onClose }) {
  const videoRef = useRef(null);
  const streamRef = useRef(null);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        if (!navigator.mediaDevices?.getUserMedia) {
          throw new Error("This browser does not support the camera.");
        }
        const stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: "user" },
          audio: false,
        });
        if (!alive) {
          stream.getTracks().forEach((t) => t.stop());
          return;
        }
        streamRef.current = stream;
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          await videoRef.current.play();
        }
        setReady(true);
      } catch (err) {
        setError(err?.message || "Could not open the camera. Allow camera access and try again.");
      }
    })();

    return () => {
      alive = false;
      streamRef.current?.getTracks().forEach((t) => t.stop());
    };
  }, []);

  async function handleScan() {
    const video = videoRef.current;
    if (!video || !video.videoWidth) {
      setError("The camera is not ready yet.");
      return;
    }

    setBusy(true);
    setError("");
    try {
      const canvas = document.createElement("canvas");
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      canvas.getContext("2d").drawImage(video, 0, 0, canvas.width, canvas.height);

      const blob = await new Promise((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.92));
      if (!blob) throw new Error("Could not capture the photo.");

      await onCapture(blob);
    } catch (err) {
      setError(err?.message || "Face verification failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className={styles.overlay}>
      <div className={styles.modal} role="dialog" aria-modal="true" aria-label={title}>
        <h2 className={styles.title}>{title}</h2>
        {hint && <p className={styles.hint}>{hint}</p>}

        <div className={styles.frame}>
          <video ref={videoRef} autoPlay muted playsInline className={styles.video} />
          <div className={styles.oval} />
          {busy && <div className={styles.scan}>Matching face...</div>}
        </div>

        {error && <p className={styles.error} role="alert">{error}</p>}

        <div className={styles.actions}>
          <button className={styles.secondary} onClick={onClose} disabled={busy}>Cancel</button>
          <button className={styles.primary} onClick={handleScan} disabled={!ready || busy}>
            {busy ? "Scanning..." : "Scan face"}
          </button>
        </div>
      </div>
    </div>
  );
}
