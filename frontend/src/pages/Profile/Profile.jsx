import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getProfile } from "../../api/mockVault";
import styles from "./Profile.module.css";

function maskPhone(phone) {
  if (!phone) return "—";
  const digits = phone.replace(/\D/g, "");
  if (digits.length < 4) return phone;
  return `${digits.slice(0, 3)}${"*".repeat(Math.max(digits.length - 6, 3))}${digits.slice(-3)}`;
}

export default function Profile() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    getProfile(user?.id, user).then((p) => {
      if (!cancelled) {
        setProfile(p);
        setIsLoading(false);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [user]);

  if (isLoading || !profile) {
    return <p className={styles.muted}>Loading profile...</p>;
  }

  const isVerified = profile.identityStatus === "VERIFIED";

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>My Profile</h1>

        <div className={styles.row}>
          <span className={styles.label}>Full Name</span>
          <span className={styles.value}>{profile.fullName}</span>
        </div>

        <div className={styles.row}>
          <span className={styles.label}>Email</span>
          <span className={styles.value}>
            {profile.email}
            {profile.emailVerified && <span className={styles.verifiedTag}>✓ Verified</span>}
          </span>
        </div>

        <div className={styles.row}>
          <span className={styles.label}>Phone</span>
          <span className={styles.value}>
            {maskPhone(profile.phone)}
            {profile.phoneVerified ? (
              <span className={styles.verifiedTag}>✓ Verified</span>
            ) : (
              <span className={styles.unverifiedTag}>Not Verified</span>
            )}
          </span>
        </div>

        <hr className={styles.divider} />

        <h2 className={styles.subtitle}>Identity Verification</h2>
        <div className={styles.statusBlock}>
          {isVerified ? (
            <span className={styles.statusOk}>✓ Verified</span>
          ) : (
            <span className={styles.statusWarn}>⚠ Not Verified</span>
          )}
        </div>

        {!isVerified && (
          <>
            <p className={styles.helpText}>
              Verify your identity to publish wills, assign beneficiaries, hire executors and
              access inherited assets.
            </p>
            <button className={styles.primaryBtn} onClick={() => navigate("/dashboard/verify-identity")}>
              Verify Identity
            </button>
          </>
        )}
      </div>
    </div>
  );
}
