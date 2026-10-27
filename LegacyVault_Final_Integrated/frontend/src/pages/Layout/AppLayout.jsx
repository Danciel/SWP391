import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getDmsAlerts } from "../../api/vaultApi";
import logo from "../../assets/logo.png";
import styles from "./AppLayout.module.css";

const NAV_ITEMS_BY_ROLE = {
  OWNER: [
    { to: "/dashboard/assets", label: "Assets" },
    { to: "/dashboard/create-will", label: "Create Will" },
    { to: "/dashboard/view-will", label: "View Will" },
    { to: "/dashboard/my-wills", label: "My Wills" },
    { to: "/dashboard/wallet", label: "Wallet" },
  ],
  EXECUTOR: [{ to: "/dashboard/executor", label: "Executor Dashboard" }],
  LEGAL_VERIFIER: [{ to: "/dashboard/verifier", label: "Legal Cases" }],
  ADMIN: [{ to: "/dashboard/admin", label: "Admin Dashboard" }],
};

export default function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  const [alerts, setAlerts] = useState([]);
  const [bellOpen, setBellOpen] = useState(false);
  const bellRef = useRef(null);
  const navItems = NAV_ITEMS_BY_ROLE[user?.role] || NAV_ITEMS_BY_ROLE.OWNER;

  useEffect(() => {
    if (user?.role !== "OWNER" && user?.role !== "USER") return undefined;
    let alive = true;
    const load = () => getDmsAlerts().then((rows) => alive && setAlerts(rows || [])).catch(() => {});
    load();
    const t = setInterval(load, 60000);
    return () => { alive = false; clearInterval(t); };
  }, [user?.role]);

  const dueAlerts = alerts.filter((a) => a.checkInDueSoon || a.missedCheckIns > 0 || a.status === "THRESHOLD_REACHED");

  useEffect(() => {
    function onClickOutside(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
      if (bellRef.current && !bellRef.current.contains(e.target)) setBellOpen(false);
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
          <div className={styles.userMenu} ref={bellRef}>
            <button className={styles.iconBtn} title="Notifications" aria-label="Notifications" onClick={() => setBellOpen((v) => !v)}>
              🔔{dueAlerts.length > 0 && <span style={{ color: "#dc2626", fontSize: 12, fontWeight: 700 }}> {dueAlerts.length}</span>}
            </button>
            {bellOpen && (
              <div className={styles.dropdown} style={{ minWidth: 280 }}>
                {dueAlerts.length === 0 && <span className={styles.dropdownItem}>No notifications</span>}
                {dueAlerts.map((a) => (
                  <NavLink key={a.willId} to={`/dashboard/my-wills/${a.willId}`} className={styles.dropdownItem} onClick={() => setBellOpen(false)}>
                    <strong>{a.willTitle}</strong>
                    <div style={{ fontSize: 12, color: "#6b7280" }}>
                      {a.label ? `${a.label} — ` : ""}
                      {a.status === "THRESHOLD_REACHED" ? "Threshold reached, executor notified" : "Check-in due. Open to check in."}
                    </div>
                  </NavLink>
                ))}
              </div>
            )}
          </div>
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
