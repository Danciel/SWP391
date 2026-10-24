
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createIdpUser } from "../../api/idpUserApi";
import "./IdpRegister.css";

export default function IdpRegister() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (e) => {
    setForm((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (
      !form.fullName.trim() ||
      !form.email.trim() ||
      !form.phoneNumber.trim()
    ) {
      setError("Please provide all the required information.");
      return;
    }

    setLoading(true);

    try {
      const result = await createIdpUser({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber.trim(),
        identityStatus: "UNVERIFIED",
        lifeStatus: "ALIVE",
      });

      if (!result?.id) {
        throw new Error("API did not return an identifier (id).");
      }

      navigate(`/idp-profile/${result.id}`);
    } catch (err) {
      setError(err.message || "Unable to create an identification profile.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="idp-register-page">
      <section className="idp-register-card">
        <h1 className="idp-register-title">
          Đăng ký hồ sơ định danh
        </h1>

        <p className="idp-register-description">
          Tạo hồ sơ trên Mock Identity Provider.
        </p>

        <form onSubmit={handleSubmit}>
          <div className="idp-register-field">
            <label htmlFor="fullName">Họ và tên</label>
            <input
              id="fullName"
              name="fullName"
              value={form.fullName}
              onChange={handleChange}
              placeholder="Nguyễn Văn A"
              autoComplete="name"
              required
            />
          </div>

          <div className="idp-register-field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
              placeholder="you@example.com"
              autoComplete="email"
              required
            />
          </div>

          <div className="idp-register-field">
            <label htmlFor="phoneNumber">Số điện thoại</label>
            <input
              id="phoneNumber"
              name="phoneNumber"
              type="tel"
              value={form.phoneNumber}
              onChange={handleChange}
              placeholder="0901234567"
              autoComplete="tel"
              required
            />
          </div>

          {error && (
            <p className="idp-register-error" role="alert">
              {error}
            </p>
          )}

          <button
            type="submit"
            className="idp-register-submit"
            disabled={loading}
          >
            {loading ? "Đang tạo hồ sơ..." : "Đăng ký hồ sơ"}
          </button>
        </form>

        <p className="idp-register-footer">
          Đã có mã định danh?{" "}
          <Link to="/idp-profile-lookup">Tra cứu hồ sơ</Link>
        </p>
      </section>
    </main>
  );
}
