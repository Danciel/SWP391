import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getDmsAlerts } from "../../api/vaultApi";
import Icon from "../../components/Icons/Icon";
import { PHOTOS } from "../../assets/images";
import logo from "../../assets/logo.png";
import styles from "./AppLayout.module.css";

const NAV_ITEMS_BY_ROLE = {
  OWNER: [
    { to: "/dashboard/assets", label: "Assets", icon: "assets" },
    { to: "/dashboard/create-will", label: "Create Will", icon: "will" },
    { to: "/dashboard/view-will", label: "View Will", icon: "eye" },
    { to: "/dashboard/my-wills", label: "My Wills", icon: "list" },
    { to: "/dashboard/wallet", label: "Wallet", icon: "wallet" },
  ],
  EXECUTOR: [
    { to: "/dashboard/executor", label: "Executor Dashboard", icon: "executor" },
    { to: "/dashboard/wallet", label: "Wallet", icon: "wallet" },
  ],
  LEGAL_VERIFIER: [{ to: "/dashboard/verifier", label: "Legal Cases", icon: "scale" }],
  ADMIN: [{ to: "/dashboard/admin", label: "Admin Dashboard", icon: "admin" }],
};

const ROLE_LABEL = { OWNER: "Vault owner", EXECUTOR: "Executor", LEGAL_VERIFIER: "Legal verifier", ADMIN: "Administrator" };

// One banner photo + short copy per section. Matched by URL prefix, first hit wins.
const SECTIONS = [
  { match: "/dashboard/assets", eyebrow: "Digital assets", title: "Everything you want protected, in one vault", photo: PHOTOS.depositBoxes, rail: "assets" },
  { match: "/dashboard/create-will", eyebrow: "Will", title: "Write down what should happen next", photo: PHOTOS.willShakespeare, pos: "50% 18%", rail: "wills" },
  { match: "/dashboard/my-wills", eyebrow: "Your wills", title: "Track every will, executor and check-in", photo: PHOTOS.will1755, rail: "wills" },
  { match: "/dashboard/view-will", eyebrow: "Inherited wills", title: "Wills that have been shared with you", photo: PHOTOS.willPatterson, rail: "wills" },
  { match: "/dashboard/wallet", eyebrow: "Wallet", title: "Balance, deposits and executor fees", photo: PHOTOS.federalVault, rail: "wallet" },
  { match: "/dashboard/profile", eyebrow: "Account", title: "Your profile and personal details", photo: PHOTOS.vaultDoor, pos: "50% 35%", rail: "account" },
  { match: "/dashboard/security", eyebrow: "Account", title: "Security PIN and sign-in protection", photo: PHOTOS.vaultDoor, pos: "50% 60%", rail: "account" },
  { match: "/dashboard/verify-identity", eyebrow: "Account", title: "Confirm who you are", photo: PHOTOS.vaultDoor, pos: "50% 45%", rail: "account" },
  { match: "/dashboard/executor", eyebrow: "Executor", title: "Requests, handovers and your pricing", photo: PHOTOS.willPatterson, wide: true },
  { match: "/dashboard/verifier", eyebrow: "Legal review", title: "Cases waiting for your decision", photo: PHOTOS.will1755, wide: true },
  { match: "/dashboard/admin", eyebrow: "Administration", title: "Operations, accounts and disputes", photo: PHOTOS.depositVault, wide: true },
];

const RAILS = {
  assets: {
    photo: PHOTOS.depositRoom,
    kicker: "Good to know",
    title: "Verify what you store",
    tips: [
      ["shield", "Connect Gmail, GitHub or Drive to verify the account really is yours."],
      ["lock", "Secrets are encrypted before they are saved."],
      ["user", "Assign an executor to each asset in your will."],
    ],
  },
  wills: {
    photo: PHOTOS.depositVault,
    kicker: "How a will works",
    title: "Executors, check-ins, handover",
    tips: [
      ["executor", "Pick executors you trust and agree their fee up front."],
      ["clock", "Check in on time; missed cycles notify your executor."],
      ["scale", "Legal verification protects beneficiaries before release."],
    ],
  },
  wallet: {
    photo: PHOTOS.depositBoxes,
    kicker: "Wallet",
    title: "Funds held in escrow",
    tips: [
      ["wallet", "Deposits cover executor fees when a handover starts."],
      ["shield", "Disputed payments are reviewed by an administrator."],
      ["check", "Every transaction is written to the audit log."],
    ],
  },
  account: {
    photo: PHOTOS.federalVault,
    kicker: "Stay safe",
    title: "Keep your account locked down",
    tips: [
      ["key", "Use a security PIN for sensitive actions."],
      ["user", "Verify your identity to unlock handover features."],
      ["bell", "Watch the bell for check-in reminders."],
    ],
  },
};

export default function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  const [alerts, setAlerts] = useState([]);
  const [bellOpen, setBellOpen] = useState(false);
  const bellRef = useRef(null);
  const navItems = NAV_ITEMS_BY_ROLE[user?.role] || NAV_ITEMS_BY_ROLE.OWNER;
  const section = SECTIONS.find((s) => pathname.startsWith(s.match));
  const rail = section?.rail ? RAILS[section.rail] : null;

  useEffect(() => {
    if (user?.role !== "OWNER" && user?.role !== "USER") return undefined;
    let alive = true;
    const load = () => getDmsAlerts().then((rows) => alive && setAlerts(rows || [])).catch(() => { });
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
  const initial = displayName.trim().charAt(0).toUpperCase();
  const roleLabel = ROLE_LABEL[user?.role] || "Member";

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
              <Icon name="bell" size={19} />
              {dueAlerts.length > 0 && <span className={styles.badge}>{dueAlerts.length}</span>}
            </button>
            {bellOpen && (
              <div className={styles.dropdown} style={{ minWidth: 300 }}>
                {dueAlerts.length === 0 && <span className={styles.dropdownItem}>No notifications</span>}
                {dueAlerts.map((a) => (
                  <NavLink key={a.willId} to={`/dashboard/my-wills/${a.willId}`} className={styles.dropdownItem} onClick={() => setBellOpen(false)}>
                    <strong>{a.willTitle}</strong>
                    <div style={{ fontSize: 12, color: "var(--muted)" }}>
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
              <span className={styles.avatar}>{initial}</span>
              <span className={styles.userText}>
                <strong>{displayName}</strong>
                <small>{roleLabel}</small>
              </span>
              <span className={styles.caret}>▾</span>
            </button>
            {menuOpen && (
              <div className={styles.dropdown}>
                <NavLink to="/dashboard/profile" className={styles.dropdownItem} onClick={() => setMenuOpen(false)}>Profile</NavLink>
                <NavLink to="/dashboard/security" className={styles.dropdownItem} onClick={() => setMenuOpen(false)}>Security</NavLink>
                <button className={styles.dropdownItem} onClick={handleLogout}>Logout</button>
              </div>
            )}
          </div>
        </div>
      </header>

      <div className={styles.body}>
        <nav className={styles.sidebar}>
          <span className={styles.navLabel}>Menu</span>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => (isActive ? `${styles.navItem} ${styles.navItemActive}` : styles.navItem)}
            >
              <Icon name={item.icon} size={18} />
              <span className={styles.navText}>{item.label}</span>
            </NavLink>
          ))}

          <div className={styles.sideCard} style={{ "--photo": `url("${PHOTOS.vaultDoor}")` }}>
            <span className={styles.sideCardIcon}><Icon name="shield" size={16} /></span>
            <strong>Protected by LegacyVault</strong>
            <small>Your assets are encrypted and every action is audited.</small>
          </div>
        </nav>

        <main className={styles.content}>
          {section && (
            <section
              className={styles.banner}
              style={{ "--photo": `url("${section.photo}")`, "--pos": section.pos || "50% 50%" }}
            >
              <div className={styles.bannerText}>
                <span className={styles.eyebrow}>{section.eyebrow}</span>
                <h1 className={styles.bannerTitle}>{section.title}</h1>
              </div>
              <span className={styles.bannerRole}><Icon name="user" size={14} /> {roleLabel}</span>
            </section>
          )}

          <div className={rail ? styles.split : styles.single}>
            <div className={styles.main}>
              <Outlet />
            </div>
            {rail && (
              <aside className={styles.rail}>
                <figure className={styles.railPhoto} style={{ "--photo": `url("${rail.photo}")` }}>
                  <figcaption>
                    <span>{rail.kicker}</span>
                    <strong>{rail.title}</strong>
                  </figcaption>
                </figure>
                <ul className={styles.tips}>
                  {rail.tips.map(([icon, text]) => (
                    <li key={text}>
                      <span className={styles.tipIcon}><Icon name={icon} size={16} /></span>
                      <span>{text}</span>
                    </li>
                  ))}
                </ul>
              </aside>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}
