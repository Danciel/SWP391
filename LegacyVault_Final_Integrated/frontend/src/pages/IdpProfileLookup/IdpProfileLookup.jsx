
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import "./IdpProfileLookup.css";

export default function IdpProfileLookup() {
  const [id, setId] = useState("");
  const navigate = useNavigate();

  const handleSubmit = (e) => {
    e.preventDefault();
    const value = id.trim();

    if (value) {
      navigate(`/idp-profile/${encodeURIComponent(value)}`);
    }
  };

  return (
    <main className="idp-lookup-page">
      <section className="idp-lookup-card">
        <h1 className="idp-lookup-title">
          Tra cứu hồ sơ định danh
        </h1>

        <p className="idp-lookup-description">
          Nhập mã ID đã được cấp khi đăng ký.
        </p>

        <form onSubmit={handleSubmit}>
          <div className="idp-lookup-field">
            <label htmlFor="externalId">
              External Identity ID
            </label>

            <input
              id="externalId"
              name="externalId"
              value={id}
              onChange={(e) => setId(e.target.value)}
              placeholder="Nhập mã định danh của bạn"
              autoComplete="off"
              required
            />
          </div>

          <button type="submit" className="idp-lookup-submit">
            Tra cứu hồ sơ
          </button>
        </form>

        <p className="idp-lookup-footer">
          Chưa có hồ sơ?{" "}
          <Link to="/idp-register">Đăng ký hồ sơ mới</Link>
        </p>
      </section>
    </main>
  );
}
