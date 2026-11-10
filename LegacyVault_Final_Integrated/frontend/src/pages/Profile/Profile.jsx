import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getProfile, getExecutorProfile, updateMyExecutorProfile } from "../../api/vaultApi";
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
  const isExecutor = user?.role === "EXECUTOR";
  const [profile, setProfile] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [executorProfile, setExecutorProfile] = useState(null);
  const [editingExecutorProfile, setEditingExecutorProfile] = useState(false);
  const [savingExecutorProfile, setSavingExecutorProfile] = useState(false);
  const [executorProfileError, setExecutorProfileError] = useState("");
  const [executorProfileMessage, setExecutorProfileMessage] = useState("");

  const [executorForm, setExecutorForm] = useState({
    bio: "",
    specialty: "",
    experienceYears: "0",
  });


  useEffect(() => {
    let cancelled = false;

    async function loadProfiles() {
      setIsLoading(true);
      setError("");
      setExecutorProfileError("");

      try {
        const account = await getProfile(user?.id, user);

        if (cancelled) return;
        setProfile(account);

        if (isExecutor) {
          const accountId =
            account.accountId ||
            account.id ||
            user?.accountId ||
            user?.id;

          if (!accountId) {
            throw new Error("Could not identify Executor account.");
          }

          const result = await getExecutorProfile(accountId);

          if (cancelled) return;

          setExecutorProfile(result);

          setExecutorForm({
            bio: result.bio || "",
            specialty: result.specialty || "",
            experienceYears: String(result.experienceYears ?? 0),
          });
        } else {
          setExecutorProfile(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message || "Could not load profile");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadProfiles();

    return () => {
      cancelled = true;
    };
  }, [user, isExecutor]);


  async function handleSaveExecutorProfile() {
    const years = Number(executorForm.experienceYears);

    if (
      !Number.isInteger(years) ||
      years < 0 ||
      years > 80
    ) {
      setExecutorProfileError(
        "Experience must be a whole number from 0 to 80."
      );
      return;
    }

    setSavingExecutorProfile(true);
    setExecutorProfileError("");
    setExecutorProfileMessage("");

    try {
      const updated = await updateMyExecutorProfile({
        bio: executorForm.bio.trim(),
        specialty: executorForm.specialty.trim(),
        experienceYears: years,
      });

      setExecutorProfile(updated);

      setExecutorForm({
        bio: updated.bio || "",
        specialty: updated.specialty || "",
        experienceYears: String(updated.experienceYears ?? 0),
      });

      setEditingExecutorProfile(false);
      setExecutorProfileMessage(
        "Executor profile updated successfully."
      );
    } catch (err) {
      setExecutorProfileError(
        err.message || "Could not update Executor profile."
      );
    } finally {
      setSavingExecutorProfile(false);
    }
  }

  if (isLoading) {
    return <p className={styles.muted}>Loading profile...</p>;
  }

  if (error) return <p role="alert" className={styles.muted}>{error}</p>;
  if (!profile) return <p className={styles.muted}>No profile information available.</p>;

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

        {isExecutor && executorProfile && (
          <>
            <hr className={styles.divider} />

            <h2 className={styles.subtitle}>
              Executor Professional Profile
            </h2>

            {executorProfileError && (
              <p role="alert" className={styles.muted}>
                {executorProfileError}
              </p>
            )}

            {executorProfileMessage && (
              <p role="status" className={styles.verifiedTag}>
                {executorProfileMessage}
              </p>
            )}

            <div className={styles.row}>
              <span className={styles.label}>Completed Cases</span>
              <span className={styles.value}>
                {executorProfile.completedCases ?? 0}
              </span>
            </div>

            <div className={styles.row}>
              <span className={styles.label}>Rating</span>
              <span className={styles.value}>
                {executorProfile.rating ?? 0}
                {" "}({executorProfile.reviewCount ?? 0} reviews)
              </span>
            </div>

            {editingExecutorProfile ? (
              <div className={styles.executorForm}>
                <label className={styles.label} htmlFor="executor-bio">
                  Introduction
                </label>
                <textarea
                  id="executor-bio"
                  maxLength={1000}
                  rows={4}
                  value={executorForm.bio}
                  onChange={(e) =>
                    setExecutorForm((prev) => ({
                      ...prev,
                      bio: e.target.value,
                    }))
                  }
                />

                <label
                  className={styles.label}
                  htmlFor="executor-specialty"
                >
                  Specialty
                </label>
                <input
                  id="executor-specialty"
                  maxLength={200}
                  value={executorForm.specialty}
                  onChange={(e) =>
                    setExecutorForm((prev) => ({
                      ...prev,
                      specialty: e.target.value,
                    }))
                  }
                />

                <label
                  className={styles.label}
                  htmlFor="executor-experience"
                >
                  Experience (years)
                </label>
                <input
                  id="executor-experience"
                  type="number"
                  min="0"
                  max="80"
                  step="1"
                  value={executorForm.experienceYears}
                  onChange={(e) =>
                    setExecutorForm((prev) => ({
                      ...prev,
                      experienceYears: e.target.value,
                    }))
                  }
                />

                <div className={styles.actionRow}>
                  <button
                    type="button"
                    className={styles.secondaryBtn}
                    disabled={savingExecutorProfile}
                    onClick={() => {
                      setExecutorForm({
                        bio: executorProfile.bio || "",
                        specialty: executorProfile.specialty || "",
                        experienceYears: String(
                          executorProfile.experienceYears ?? 0
                        ),
                      });
                      setEditingExecutorProfile(false);
                      setExecutorProfileError("");
                    }}
                  >
                    Cancel
                  </button>

                  <button
                    type="button"
                    className={styles.primaryBtn}
                    disabled={savingExecutorProfile}
                    onClick={handleSaveExecutorProfile}
                  >
                    {savingExecutorProfile ? "Saving..." : "Save Profile"}
                  </button>
                </div>
              </div>
            ) : (
              <>
                <div className={styles.row}>
                  <span className={styles.label}>Introduction</span>
                  <span className={styles.value}>
                    {executorProfile.bio || "Not provided"}
                  </span>
                </div>

                <div className={styles.row}>
                  <span className={styles.label}>Specialty</span>
                  <span className={styles.value}>
                    {executorProfile.specialty || "Not provided"}
                  </span>
                </div>

                <div className={styles.row}>
                  <span className={styles.label}>Experience</span>
                  <span className={styles.value}>
                    {executorProfile.experienceYears ?? 0} years
                  </span>
                </div>

                <button
                  type="button"
                  className={styles.primaryBtn}
                  onClick={() => {
                    setExecutorProfileError("");
                    setExecutorProfileMessage("");
                    setEditingExecutorProfile(true);
                  }}
                >
                  Edit Professional Profile
                </button>
              </>
            )}
          </>
        )}

      </div>
    </div>
  );
}
