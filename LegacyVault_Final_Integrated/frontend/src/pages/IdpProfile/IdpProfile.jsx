
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getIdpUser } from "../../api/idpUserApi";
import "./IdpProfile.css";

export default function IdpProfile() {
  const { id } = useParams();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let active = true;

    async function loadProfile() {
      setLoading(true);
      setError("");

      try {
        const result = await getIdpUser(id);
        if (active) setProfile(result);
      } catch (err) {
        if (active) {
          setError(err.message || "Không thể tải hồ sơ định danh.");
        }
      } finally {
        if (active) setLoading(false);
      }
    }

    loadProfile();

    return () => {
      active = false;
    };
  }, [id]);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(profile.id);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      setError("Không thể sao chép mã ID. Vui lòng sao chép thủ công.");
    }
  };

  if (loading) {
    return (
      <main className="idp-profile-page">
        <p>Đang tải hồ sơ...</p>
      </main>
    );
  }

  if (error) {
    return (
      <main className="idp-profile-page">
        <section className="idp-profile-card">
          <p className="idp-profile-error" role="alert">{error}</p>
          <Link className="idp-profile-link" to="/idp/register">
            Đăng ký hồ sơ mới
          </Link>
        </section>
      </main>
    );
  }

  if (!profile) {
    return (
      <main className="idp-profile-page">
        <p>Không tìm thấy hồ sơ.</p>
      </main>
    );
  }

  return (
    <main className="idp-profile-page">
      <section className="idp-profile-card">
        <h1 className="idp-profile-title">Hồ sơ định danh</h1>
        <p className="idp-profile-description">
          Hồ sơ đã được tạo thành công.
        </p>

        <label className="idp-profile-label">
          Mã định danh (External Identity ID)
        </label>

        <div className="idp-profile-id-box">
          <code className="idp-profile-id">{profile.id}</code>
          <button
            type="button"
            onClick={handleCopy}
            className="idp-profile-copy-button"
          >
            {copied ? "Đã sao chép" : "Sao chép"}
          </button>
        </div>

        <div className="idp-profile-details">
          <p><strong>Họ tên:</strong> {profile.fullName}</p>
          <p><strong>Email:</strong> {profile.email}</p>
          <p><strong>Số điện thoại:</strong> {profile.phoneNumber}</p>
          <p>
            <strong>Trạng thái danh tính:</strong>{" "}
            {profile.identityStatus}
          </p>
          <p><strong>LifeStatus:</strong> {profile.lifeStatus}</p>
          <p>
            <strong>Face ID:</strong>{" "}
            {profile.faceId || "Chưa đăng ký khuôn mặt"}
          </p>
        </div>

        <p className="idp-profile-note">
          Hãy lưu mã định danh để sử dụng trong bước liên kết với LegacyVault.
        </p>

        <Link className="idp-profile-link" to="/idp/register">
          Tạo hồ sơ khác
        </Link>
      </section>
    </main>
  );
}
