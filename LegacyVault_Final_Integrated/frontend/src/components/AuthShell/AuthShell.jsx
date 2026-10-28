import Icon from "../Icons/Icon";
import { PHOTOS } from "../../assets/images";
import styles from "./AuthShell.module.css";

const FEATURES = [
  { icon: "lock", title: "Encrypted asset vault", text: "Accounts, documents and cloud storage kept under your control." },
  { icon: "clock", title: "Dead-man's switch", text: "Regular check-ins decide when your legacy is released." },
  { icon: "executor", title: "Verified executors", text: "Trusted people and legal review hand everything over safely." },
];

export default function AuthShell({ children }) {
  return (
    <div className={styles.shell}>
      <aside className={styles.hero} style={{ "--photo": `url("${PHOTOS.vaultDoor}")` }}>
        <div className={styles.heroInner}>
          <span className={styles.badge}>
            <Icon name="shield" size={14} /> LegacyVault
          </span>
          <h2 className={styles.headline}>Your legacy, locked in and handed down with care.</h2>
          <p className={styles.lead}>
            Store digital assets, write your will and name the people who carry it forward.
          </p>
          <ul className={styles.features}>
            {FEATURES.map((f) => (
              <li key={f.title}>
                <span className={styles.featIcon}><Icon name={f.icon} size={18} /></span>
                <span>
                  <strong>{f.title}</strong>
                  <small>{f.text}</small>
                </span>
              </li>
            ))}
          </ul>
        </div>
      </aside>
      <main className={styles.formSide}>{children}</main>
    </div>
  );
}
