
import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { enrollIdpFace } from "../../api/idpUserApi";
import "./IdpFaceEnroll.css";

export default function IdpFaceEnroll() {
  const { id } = useParams();
  const videoRef = useRef(null);
  const streamRef = useRef(null);

  const [preview, setPreview] = useState("");
  const [cameraError, setCameraError] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let active = true;

    async function startCamera() {
      try {
        if (!navigator.mediaDevices?.getUserMedia) {
          throw new Error("Trình duyệt không hỗ trợ truy cập camera.");
        }

        const stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: "user" },
          audio: false,
        });

        if (!active) {
          stream.getTracks().forEach((track) => track.stop());
          return;
        }

        streamRef.current = stream;

        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          await videoRef.current.play();
        }
      } catch (error) {
        if (active) {
          setCameraError(
            error.name === "NotAllowedError"
              ? "Bạn đã từ chối quyền camera. Hãy cho phép camera trong trình duyệt rồi thử lại."
              : error.message || "Không thể mở camera."
          );
        }
      }
    }

    startCamera();

    return () => {
      active = false;
      streamRef.current?.getTracks().forEach((track) => track.stop());
    };
  }, []);

  function capturePhoto() {
    const video = videoRef.current;

    if (!video || !video.videoWidth || !video.videoHeight) {
      setCameraError("Camera chưa sẵn sàng. Vui lòng thử lại.");
      return;
    }

    const canvas = document.createElement("canvas");
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    const context = canvas.getContext("2d");
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    canvas.toBlob(
      (blob) => {
        if (!blob) {
          setCameraError("Không thể chụp ảnh. Vui lòng thử lại.");
          return;
        }

        setPreview(URL.createObjectURL(blob));
        setMessage("");
        setCameraError("");

        setPhotoFile(
          new File([blob], "face-enrollment.jpg", {
            type: "image/jpeg",
          })
        );
      },
      "image/jpeg",
      0.9
    );
  }

  const [photoFile, setPhotoFile] = useState(null);

  async function handleEnroll() {
    if (!photoFile) {
      setCameraError("Vui lòng chụp ảnh trước khi đăng ký.");
      return;
    }

    try {
      setLoading(true);
      setMessage("");
      setCameraError("");

      await enrollIdpFace(id, photoFile);

      setMessage("Đăng ký khuôn mặt thành công.");
    } catch (error) {
      setCameraError(error.message || "Đăng ký khuôn mặt thất bại.");
    } finally {
      setLoading(false);
    }
  }

  function retakePhoto() {
    if (preview) URL.revokeObjectURL(preview);
    setPreview("");
    setPhotoFile(null);
    setMessage("");
    setCameraError("");
  }

  useEffect(() => {
    return () => {
      if (preview) URL.revokeObjectURL(preview);
    };
  }, [preview]);

  return (
    <main className="face-enroll-page">
      <section className="face-enroll-card">
        <h1>Đăng ký khuôn mặt</h1>
        <p className="face-enroll-description">
          Chụp ảnh khuôn mặt để lưu vào hồ sơ định danh.
        </p>

        <p className="face-enroll-id">
          <strong>External Identity ID:</strong>
          <br />
          {id}
        </p>

        {preview ? (
          <img
            className="face-enroll-preview"
            src={preview}
            alt="Ảnh khuôn mặt đã chụp"
          />
        ) : (
          <video
            ref={videoRef}
            className="face-enroll-video"
            autoPlay
            muted
            playsInline
          />
        )}

        {cameraError && (
          <p className="face-enroll-error" role="alert">
            {cameraError}
          </p>
        )}

        {message && (
          <p className="face-enroll-success" role="status">
            {message}
          </p>
        )}

        <div className="face-enroll-actions">
          {!preview ? (
            <button type="button" onClick={capturePhoto}>
              Chụp ảnh
            </button>
          ) : (
            <>
              <button
                type="button"
                className="face-enroll-secondary"
                onClick={retakePhoto}
                disabled={loading}
              >
                Chụp lại
              </button>

              <button
                type="button"
                onClick={handleEnroll}
                disabled={loading}
              >
                {loading ? "Đang đăng ký..." : "Đăng ký khuôn mặt"}
              </button>
            </>
          )}
        </div>

        <Link to={`/idp-profile/${encodeURIComponent(id)}`}>
          Quay lại hồ sơ định danh
        </Link>
      </section>
    </main>
  );
}
