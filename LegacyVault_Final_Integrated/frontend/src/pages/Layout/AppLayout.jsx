import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import logo from "../../assets/logo.png";
import styles from "./AppLayout.module.css";

const USER_NAV = [
  { to: "/dashboard/assets", label: "Assets" },
  { to: "/dashboard/create-will", label: "Create Will" },
  { to: "/dashboard/view-will", label: "View Will" },
  { to: "/dashboard/my-wills", label: "My Wills" },
];

const NAV_ITEMS_BY_ROLE = {
  USER: USER_NAV,
  OWNER: USER_NAV,
  /* legacy frontend compatibility */
  LEGACY_OWNER: USER_NAV,
  EXECUTOR: [{ to: "/dashboard/executor", label: "Executor Dashboard" }],
  LEGAL_VERIFIER: [{ to: "/dashboard/verifier", label: "Legal Cases" }],
  ADMIN: [{ to: "/dashboard/admin", label: "Admin Dashboard" }],
};

export default function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  const navItems = NAV_ITEMS_BY_ROLE[user?.role] || USER_NAV;

  useEffect(() => {
    function onClickOutside(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
    }
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  function handleLogout() {
    logout();
    navigate("/login");
  }

  const displayName = user?.fullName || "John Nguyen";

  return (
    <div className={styles.shell}>
      <header className={styles.topbar}>
        <div className={styles.brand}>
          <img src={logo} alt="LegacyVault" className={styles.logo} />
          <span className={styles.brandName}>LegacyVault</span>
        </div>
        <div className={styles.topbarRight}>
          <button className={styles.iconBtn} title="Notifications" aria-label="Notifications">
            🔔
          </button>
          <div className={styles.userMenu} ref={menuRef}>
            <button className={styles.userBtn} onClick={() => setMenuOpen((v) => !v)}>
              {displayName} <span className={styles.caret}>▾</span>
            </button>
            {menuOpen && (
              <div className={styles.dropdown}>
                <NavLink
                  to="/dashboard/profile"
                  className={styles.dropdownItem}
                  onClick={() => setMenuOpen(false)}
                >
                  Profile
                </NavLink>
                <NavLink
                  to="/dashboard/profile"
                  className={styles.dropdownItem}
                  onClick={() => setMenuOpen(false)}
                >
                  Security
                </NavLink>
                <button className={styles.dropdownItem} onClick={handleLogout}>
                  Logout
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      <div className={styles.body}>
        <nav className={styles.sidebar}>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                isActive ? `${styles.navItem} ${styles.navItemActive}` : styles.navItem
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
