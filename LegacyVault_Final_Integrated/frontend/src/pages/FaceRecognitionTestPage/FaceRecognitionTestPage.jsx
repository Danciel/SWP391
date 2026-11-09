// src/pages/FaceRecognitionTestPage.jsx
import { useEffect, useRef, useState } from "react";
import { enrollFace, verifyFace, deleteFace } from "../../api/faceRecognitionApi";
import { getToken } from "../../api/client";
import { ApiError } from "../../api/client";

export default function FaceRecognitionTestPage() {
    const videoRef = useRef(null);
    const canvasRef = useRef(null);
    const streamRef = useRef(null);

    const [userId, setUserId] = useState("");
    const [cameraReady, setCameraReady] = useState(false);
    const [cameraError, setCameraError] = useState(null);

    const [captured, setCaptured] = useState(null);
    const [previewUrl, setPreviewUrl] = useState(null);

    const [busy, setBusy] = useState(false);
    const [logs, setLogs] = useState([]);
    const [lastResult, setLastResult] = useState(null);

    // ---------- logging ----------
    const log = (level, ...args) => {
        const text = args
            .map((a) => (typeof a === "string" ? a : JSON.stringify(a)))
            .join(" ");
        const entry = `${new Date().toLocaleTimeString()} [${level}] ${text}`;
        console.log("[FaceTestPage]", level, ...args);
        setLogs((prev) => [...prev.slice(-199), entry]);
    };

    // ---------- camera (unchanged) ----------
    useEffect(() => {
        let cancelled = false;

        async function start() {
            if (!navigator.mediaDevices?.getUserMedia) {
                setCameraError("getUserMedia is not supported in this browser.");
                log("ERROR", "getUserMedia not supported");
                return;
            }
            try {
                const stream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        width: { ideal: 640 },
                        height: { ideal: 480 },
                        facingMode: "user",
                    },
                    audio: false,
                });
                if (cancelled) {
                    stream.getTracks().forEach((t) => t.stop());
                    return;
                }
                streamRef.current = stream;
                if (videoRef.current) {
                    videoRef.current.srcObject = stream;
                    await videoRef.current.play().catch(() => {});
                    setCameraReady(true);
                    log("INFO", "Camera started");
                }
            } catch (err) {
                setCameraError(err.message || String(err));
                log("ERROR", "Camera start failed:", err.message || err);
            }
        }

        start();
        return () => {
            cancelled = true;
            if (streamRef.current) {
                streamRef.current.getTracks().forEach((t) => t.stop());
                streamRef.current = null;
            }
        };
    }, []);

    // ---------- capture (unchanged) ----------
    const capture = () => {
        const video = videoRef.current;
        const canvas = canvasRef.current;
        if (!video || !canvas) return;

        const w = video.videoWidth || 640;
        const h = video.videoHeight || 480;
        canvas.width = w;
        canvas.height = h;
        canvas.getContext("2d").drawImage(video, 0, 0, w, h);

        canvas.toBlob(
            (blob) => {
                if (!blob) return log("ERROR", "toBlob returned null");
                if (previewUrl) URL.revokeObjectURL(previewUrl);
                const url = URL.createObjectURL(blob);
                setCaptured(blob);
                setPreviewUrl(url);
                log("INFO", `Captured ${blob.size} bytes (${w}x${h})`);
            },
            "image/jpeg",
            0.92
        );
    };

    const clearPreview = () => {
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        setPreviewUrl(null);
        setCaptured(null);
        log("INFO", "Cleared captured image");
    };

    // ---------- actions (rewritten) ----------
    // Api calls now THROW on non-2xx, so we try/catch and log the ApiError
    // in one place. Same shape for all three.
    const handleApiCall = async (op, fn) => {
        if (!userId.trim()) {
            log("WARN", "Missing IdpUser.Id");
            return;
        }
        setBusy(true);
        setLastResult(null);
        try {
            log("INFO", `${op}: calling for ${userId.trim()}`);
            const data = await fn(userId.trim());
            log("OK", `${op} ->`, data);
            setLastResult({ op, ok: true, data });
        } catch (err) {
            if (err instanceof ApiError) {
                log(
                    "FAIL",
                    `${op} HTTP ${err.status}: ${err.message}`,
                    err.payload
                );
                setLastResult({
                    op,
                    ok: false,
                    status: err.status,
                    message: err.message,
                    payload: err.payload,
                });
            } else {
                log("ERROR", `${op} threw:`, err.message || String(err));
                setLastResult({ op, ok: false, message: err.message || String(err) });
            }
        } finally {
            setBusy(false);
        }
    };

    const runEnroll = () => {
        if (!captured) return log("WARN", "Capture an image first");
        return handleApiCall("enroll", (id) => enrollFace(id, captured));
    };

    const runVerify = () => {
        if (!captured) return log("WARN", "Capture an image first");
        return handleApiCall("verify", (id) => verifyFace(id, captured));
    };

    const runDelete = () =>
        handleApiCall("delete", (id) => deleteFace(id));

    // ---------- render ----------
    const tokenPresent = Boolean(getToken());

    return (
        <div style={styles.page}>
            <h1 style={{ marginTop: 0 }}>Face Recognition Test</h1>

            <div style={styles.row}>
                <label style={styles.label}>IdpUser.Id</label>
                <input
                    style={styles.input}
                    placeholder="paste a GUID from the IdpUsers table"
                    value={userId}
                    onChange={(e) => setUserId(e.target.value)}
                    disabled={busy}
                />
                <span
                    style={{
                        ...styles.badge,
                        background: tokenPresent ? "#123d1f" : "#3d1212",
                        color: tokenPresent ? "#7ee08a" : "#ff8c8c",
                    }}
                    title={
                        tokenPresent
                            ? "Authorization: Bearer found in localStorage (lv_token)"
                            : "No token found — log in as ADMIN first"
                    }
                >
          {tokenPresent ? "JWT ✓" : "JWT ✗"}
        </span>
            </div>

            <div style={styles.cameraRow}>
                <div style={styles.videoBox}>
                    {cameraError ? (
                        <div style={styles.error}>Camera error: {cameraError}</div>
                    ) : (
                        <video ref={videoRef} muted playsInline style={styles.video} />
                    )}
                    <div style={styles.hint}>
                        {cameraReady ? "Camera live" : "Waiting for camera…"}
                    </div>
                </div>

                <div style={styles.videoBox}>
                    {previewUrl ? (
                        <img src={previewUrl} alt="captured" style={styles.video} />
                    ) : (
                        <div style={styles.placeholder}>No capture yet</div>
                    )}
                    <div style={styles.hint}>Captured frame</div>
                </div>
            </div>

            <div style={styles.btnRow}>
                <button
                    style={styles.btn}
                    onClick={capture}
                    disabled={!cameraReady || busy}
                >
                    📸 Capture
                </button>
                <button
                    style={styles.btn}
                    onClick={clearPreview}
                    disabled={!captured || busy}
                >
                    🗑️ Clear capture
                </button>
                <button
                    style={styles.btnPrimary}
                    onClick={runEnroll}
                    disabled={busy || !captured || !userId}
                >
                    {busy ? "Working…" : "✅ Enroll"}
                </button>
                <button
                    style={styles.btnPrimary}
                    onClick={runVerify}
                    disabled={busy || !captured || !userId}
                >
                    {busy ? "Working…" : "🔍 Verify"}
                </button>
                <button
                    style={styles.btnDanger}
                    onClick={runDelete}
                    disabled={busy || !userId}
                >
                    ❌ Delete face
                </button>
            </div>

            {lastResult && (
                <div style={styles.result}>
                    <strong>
                        {lastResult.ok ? "✓" : "✗"} Last result ({lastResult.op}
                        {lastResult.status ? ` · HTTP ${lastResult.status}` : ""})
                    </strong>
                    <pre style={styles.pre}>
            {JSON.stringify(lastResult, null, 2)}
          </pre>
                </div>
            )}

            <div style={styles.logHeader}>
                <strong>Log</strong>
                <button style={styles.smallBtn} onClick={() => setLogs([])}>
                    clear
                </button>
            </div>
            <div style={styles.logBox}>
                {logs.length === 0 ? (
                    <div style={{ opacity: 0.6 }}>No log entries yet.</div>
                ) : (
                    logs.map((line, i) => (
                        <div key={i} style={styles.logLine}>
                            {line}
                        </div>
                    ))
                )}
            </div>

            <canvas ref={canvasRef} style={{ display: "none" }} />
        </div>
    );
}

// ---------- inline styles (identical to before + one new badge style) ----------
const styles = {
    page: {
        padding: 24,
        fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
        maxWidth: 960,
        margin: "0 auto",
        color: "#e6e6e6",
        background: "#141414",
        minHeight: "100vh",
        boxSizing: "border-box",
    },
    row: { display: "flex", gap: 12, alignItems: "center", marginBottom: 16 },
    label: { width: 110, fontSize: 13, opacity: 0.85 },
    input: {
        flex: 1,
        padding: "8px 10px",
        borderRadius: 6,
        border: "1px solid #333",
        background: "#1d1d1d",
        color: "#e6e6e6",
        fontFamily: "inherit",
    },
    badge: {
        padding: "4px 8px",
        borderRadius: 6,
        fontSize: 11,
        fontFamily: "inherit",
    },
    cameraRow: { display: "flex", gap: 16, marginBottom: 16 },
    videoBox: {
        flex: 1,
        background: "#0b0b0b",
        border: "1px solid #333",
        borderRadius: 8,
        overflow: "hidden",
        position: "relative",
        aspectRatio: "4 / 3",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
    },
    video: { width: "100%", height: "100%", objectFit: "cover" },
    placeholder: { opacity: 0.5 },
    hint: {
        position: "absolute",
        bottom: 6,
        left: 8,
        fontSize: 11,
        background: "rgba(0,0,0,0.6)",
        padding: "2px 6px",
        borderRadius: 4,
    },
    error: { color: "#ff8080", padding: 12 },
    btnRow: { display: "flex", gap: 10, flexWrap: "wrap", marginBottom: 16 },
    btn: {
        padding: "8px 14px",
        borderRadius: 6,
        border: "1px solid #444",
        background: "#242424",
        color: "#e6e6e6",
        cursor: "pointer",
    },
    btnPrimary: {
        padding: "8px 14px",
        borderRadius: 6,
        border: "1px solid #2a5d3a",
        background: "#173a24",
        color: "#d5f5dc",
        cursor: "pointer",
    },
    btnDanger: {
        padding: "8px 14px",
        borderRadius: 6,
        border: "1px solid #5d2a2a",
        background: "#3a1717",
        color: "#f5d5d5",
        cursor: "pointer",
    },
    result: { marginBottom: 16, fontSize: 13 },
    pre: {
        background: "#0b0b0b",
        border: "1px solid #333",
        borderRadius: 6,
        padding: 10,
        overflow: "auto",
        maxHeight: 220,
        fontSize: 12,
    },
    logHeader: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        marginBottom: 6,
    },
    logBox: {
        background: "#0b0b0b",
        border: "1px solid #333",
        borderRadius: 6,
        padding: 10,
        height: 220,
        overflow: "auto",
        fontSize: 12,
    },
    logLine: { whiteSpace: "pre-wrap", lineHeight: 1.4 },
    smallBtn: {
        padding: "2px 8px",
        borderRadius: 4,
        border: "1px solid #444",
        background: "#242424",
        color: "#e6e6e6",
        cursor: "pointer",
        fontSize: 11,
    },
};