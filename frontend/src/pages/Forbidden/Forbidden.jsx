import { useEffect, useRef, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import logo from "../../assets/logo.png";
import layoutStyles from "../Layout/AppLayout.module.css";
import styles from "./Forbidden.module.css";

export default function Forbidden() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  useEffect(() => {
    function onClickOutside(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        setMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", onClickOutside);

    return () => {
      document.removeEventListener("mousedown", onClickOutside);
    };
  }, []);

  function handleLogout() {
    logout();
    navigate("/login");
  }

  function handleBackToDashboard() {
    const roleHome = {
      OWNER: "/dashboard/assets",
      EXECUTOR: "/dashboard/executor",
      BENEFICIARY: "/dashboard/view-will",
      LEGAL_VERIFIER: "/dashboard/verifier",
      ADMIN: "/dashboard/admin",
    };

    navigate(roleHome[user?.role] || "/");
  }

  const displayName = user?.fullName || "John Nguyen";

  return (
    <div className={styles.page}>
      {/* Header lấy theo AppLayout hiện tại */}
      <header className={layoutStyles.topbar}>
        <div className={layoutStyles.brand}>
          <img
            src={logo}
            alt="LegacyVault"
            className={layoutStyles.logo}
          />

          <span className={layoutStyles.brandName}>
            LegacyVault
          </span>
        </div>

        <div className={layoutStyles.topbarRight}>
          <button
            className={layoutStyles.iconBtn}
            title="Notifications"
            aria-label="Notifications"
          >
            🔔
          </button>

          <div
            className={layoutStyles.userMenu}
            ref={menuRef}
          >
            <button
              className={layoutStyles.userBtn}
              onClick={() => setMenuOpen((v) => !v)}
            >
              {displayName}
              <span className={layoutStyles.caret}>▾</span>
            </button>

            {menuOpen && (
              <div className={layoutStyles.dropdown}>
                <NavLink
                  to="/dashboard/profile"
                  className={layoutStyles.dropdownItem}
                  onClick={() => setMenuOpen(false)}
                >
                  Profile
                </NavLink>

                <NavLink
                  to="/dashboard/profile"
                  className={layoutStyles.dropdownItem}
                  onClick={() => setMenuOpen(false)}
                >
                  Security
                </NavLink>

                <button
                  className={layoutStyles.dropdownItem}
                  onClick={handleLogout}
                >
                  Logout
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Nội dung trang 403 */}
      <main className={styles.content}>
        <h1 className={styles.title}>Access Denied</h1>

        <p className={styles.message}>
          You do not have permission to access this page.
        </p>

        <button
          type="button"
          className={styles.dashboardButton}
          onClick={handleBackToDashboard}
        >
          Back to Dashboard
        </button>
      </main>
    </div>
  );
}