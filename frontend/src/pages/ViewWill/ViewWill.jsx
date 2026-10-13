import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getInheritedWills } from "../../api/mockVault";
import {
  getBeneficiaryInvitationsByEmail,
  seedBeneficiaryInvitationForDemo,
  updateBeneficiaryInvitationStatus,
} from "../../api/mockBackend";

import styles from "./ViewWill.module.css";

export default function ViewWill() {
  const { user } = useAuth();

  const [wills, setWills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [invitations, setInvitations] = useState([]);

  useEffect(() => {
    let isMounted = true;

    async function loadData() {
      try {
        const data = await getInheritedWills(user?.id);

        if (isMounted) {
          setWills(data);
        }
      } catch (error) {
        console.error("Failed to load inherited wills:", error);
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }

      try {
        if (user?.email) {
          seedBeneficiaryInvitationForDemo(user.email);

          const invitationData = getBeneficiaryInvitationsByEmail(
            user.email
          );

          if (isMounted) {
            setInvitations(invitationData);
          }
        }
      } catch (error) {
        console.error(
          "Failed to load beneficiary invitations:",
          error
        );
      }
    }

    loadData();

    return () => {
      isMounted = false;
    };
  }, [user?.id, user?.email]);

  async function handleInvitationResponse(invitationId, status) {
    try {
      updateBeneficiaryInvitationStatus(invitationId, status);

      const updatedInvitations = getBeneficiaryInvitationsByEmail(
        user?.email
      );

      setInvitations(updatedInvitations);
    } catch (error) {
      console.error("Failed to update invitation:", error);
    }
  }

  const acceptedAssignments = invitations.filter(
    (invitation) => invitation.status === "ACCEPTED"
  );

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Inherited Wills</h1>
      <p className={styles.subtitle}>
        View inheritance cases assigned to you.
      </p>

      {/* Inherited Wills */}


      <section className={styles.inheritedSection}>
        {loading ? (
          <p className={styles.muted}>Loading...</p>
        ) : wills.length === 0 ? (
          <p className={styles.muted}>
            No inheritance cases assigned to you yet.
          </p>
        ) : (
          <div className={styles.inheritedList}>
            {wills.map((w) => (
              <div key={w.id} className={styles.inheritedCard}>
                <div className={styles.inheritedCardHeader}>
                  <div>
                    <span className={styles.inheritedLabel}>
                      INHERITANCE RECORD
                    </span>
                    <h3 className={styles.willName}>
                      {w.willName}
                    </h3>
                  </div>

                  <span
                    className={
                      w.claimStatus === "CLAIMED"
                        ? styles.claimedTag
                        : styles.readyTag
                    }
                  >
                    {w.claimStatus.replaceAll("_", " ")}
                  </span>
                </div>

                <div className={styles.inheritedInfo}>
                  <div className={styles.inheritedInfoRow}>
                    <span>From</span>
                    <strong>{w.from}</strong>
                  </div>

                  <div className={styles.inheritedInfoRow}>
                    <span>Assigned Assets</span>
                    <strong>{w.assets.length} assets</strong>
                  </div>

                  <div className={styles.inheritedInfoRow}>
                    <span>Claim Status</span>
                    <strong>{w.claimStatus.replaceAll("_", " ")}</strong>
                  </div>
                </div>

                <div className={styles.inheritedFooter}>
                  <Link
                    className={styles.viewBtn}
                    to={`/dashboard/view-will/${w.id}`}
                  >
                    View Details →
                  </Link>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Beneficiary Invitations */}
      <section>
        <h2 className={styles.title}>Beneficiary Invitations</h2>

        <p className={styles.subtitle}>
          Review invitations to receive assigned digital assets.
        </p>


        {invitations.length === 0 ? (
          <p className={styles.muted}>
            No invitations received yet.
          </p>
        ) : (
          <div className={styles.list}>
            {invitations.map((invitation) => (
              <div
                key={invitation.id}
                className={styles.card}
              >
                <div className={styles.cardHead}>
                  <h3 className={styles.willName}>
                    {invitation.willTitle || "Untitled Will"}
                  </h3>

                  <span
                    className={`${styles.invitationStatus} ${invitation.status === "PENDING"
                      ? styles.pendingStatus
                      : invitation.status === "ACCEPTED"
                        ? styles.acceptedStatus
                        : styles.declinedStatus
                      }`}
                  >
                    {invitation.status.replaceAll("_", " ")}
                  </span>
                </div>

                <p className={styles.metaLine}>
                  From:{" "}
                  {invitation.ownerName || invitation.ownerEmail}
                </p>

                <p className={styles.metaLine}>
                  Asset: {invitation.assetName || "Unnamed asset"}
                </p>

                <p className={styles.metaLine}>
                  Sent:{" "}
                  {new Date(
                    invitation.createdAt
                  ).toLocaleDateString()}
                </p>

                {invitation.status === "PENDING" && (
                  <div className={styles.invitationActions}>
                    <button
                      type="button"
                      className={styles.acceptBtn}
                      onClick={() =>
                        handleInvitationResponse(invitation.id, "ACCEPTED")
                      }
                    >
                      Accept
                    </button>

                    <button
                      type="button"
                      className={styles.declineBtn}
                      onClick={() =>
                        handleInvitationResponse(invitation.id, "DECLINED")
                      }
                    >
                      Decline
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Assigned Assets */}
      <section>
        <h2 className={styles.title}>Assigned Assets</h2>
        <p className={styles.subtitle}>
          Track assets assigned to you by their owners.
        </p>

        {acceptedAssignments.length === 0 ? (
          <p className={styles.muted}>
            No assigned assets yet.
          </p>
        ) : (
          <div className={styles.list}>
            {acceptedAssignments.map((invitation) => (
              <div
                key={`assignment-${invitation.id}`}
                className={styles.card}
              >
                <div className={styles.cardHead}>
                  <h3 className={styles.willName}>
                    {invitation.assetName || "Unnamed asset"}
                  </h3>

                  <span
                    className={`${styles.invitationStatus} ${invitation.status === "PENDING"
                      ? styles.pendingStatus
                      : invitation.status === "ACCEPTED"
                        ? styles.acceptedStatus
                        : styles.declinedStatus
                      }`}
                  >
                    {invitation.status.replaceAll("_", " ")}
                  </span>
                </div>

                <p className={styles.metaLine}>
                  Owner: {invitation.ownerName || invitation.ownerEmail}
                </p>

                <p className={styles.metaLine}>
                  Will: {invitation.willTitle || "Untitled Will"}
                </p>

                <p className={styles.metaLine}>
                  Assigned on:{" "}
                  {new Date(invitation.createdAt).toLocaleDateString()}
                </p>
              </div>
            ))}
          </div>
        )}
      </section>

      
    </div>
  );
}