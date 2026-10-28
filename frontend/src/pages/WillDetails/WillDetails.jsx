
import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import SecurityPinModal from "../../components/SecurityPinModal/SecurityPinModal";

import {
  getWill,
  getWills,
  getAssets,
  checkInDms,
  cancelDeathHandover,
  requestImmediateHandover,
  getExecutors,
} from "../../api/vaultApi";
<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
import { providerLabel, verificationInfo } from "../../utils/assetCatalog";
import { dmsMessage, dmsState, fmtDate } from "../../utils/willHelpers";
=======

import {
  providerLabel,
  verificationInfo,
} from "../../utils/assetCatalog";

import {
  dmsMessage,
  dmsState,
  fmtDate,
  specialistFor,
} from "../../utils/willHelpers";

import { useBackdropClose } from "../../utils/useBackdropClose";
import ExecutorFeeInsight from "../../components/ExecutorFeeInsight/ExecutorFeeInsight";
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx
import styles from "./WillDetails.module.css";

// LV-17: Overview / Assets / Beneficiaries / Executor / DMS are shown together on one page.
// Backend allows editing a will only while it is DRAFT (WillService.EnsureDraft), so editing lives in the
// create-will wizard; this page is the read view plus the actions the backend allows after submit (check-in).

const EXECUTOR_STATUS_LABEL = {
  PENDING: "Waiting for the executor to accept",
  ACCEPTED: "Accepted",
  DECLINED: "Declined",
};

export default function WillDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [will, setWill] = useState(null);
  const [assets, setAssets] = useState([]);
  const [executors, setExecutors] = useState([]);

  const [loadError, setLoadError] = useState("");

  // checking: Kiem tra Will trong danh sach Owner
  // pin:      Can xac minh PIN
  // ready:    Da duoc phep xem Will
  // error:    Loi khi tai Will
  const [accessState, setAccessState] = useState("checking");

  // Grant chi luu tam thoi trong React state
  const [pinGrant, setPinGrant] = useState(null);

  // Tranh response cu ghi de khi doi Will
  const requestVersion = useRef(0);

  const [openAssets, setOpenAssets] = useState(new Set());

  const [confirmingHandover, setConfirmingHandover] =
    useState(false);
  const [handoverBusy, setHandoverBusy] = useState(false);
  const [handoverError, setHandoverError] = useState("");

  const [checkInBusy, setCheckInBusy] = useState(false);
  const [checkInError, setCheckInError] = useState("");
  const [checkInDone, setCheckInDone] = useState("");
<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
=======

  const [handover, setHandover] = useState(null);

  const [review, setReview] = useState({
    rating: 5,
    comment: "",
    busy: false,
    error: "",
    done: false,
  });
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx

  const handoverBackdrop = useBackdropClose(() => {
    if (!handoverBusy) {
      setConfirmingHandover(false);
    }
  });

  // ==========================================
  // B1. KIEM TRA WILL TRUOC KHI HIEN CHI TIET
  // ==========================================
  useEffect(() => {
    const version = ++requestVersion.current;

    setWill(null);
    setAssets([]);
    setExecutors([]);
    setHandover(null);
    setPinGrant(null);
    setLoadError("");
<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
    try {
      const [willData, assetsData] = await Promise.all([getWill(user?.id, id), getAssets(user?.id)]);
      setWill(willData);
      setAssets(assetsData);
      getExecutors().then(setExecutors).catch(() => setExecutors([]));
    } catch (err) {
      setLoadError(err.message || "Could not load will details");
    } finally {
      setLoading(false);
    }
  }

  if (loading) return <p className={styles.muted}>Loading...</p>;
  if (loadError) return <p role="alert" className={styles.muted}>{loadError}</p>;
  if (!will) return <p className={styles.muted}>Will not available.</p>;

  const isDraft = will.status === "DRAFT";
  const dms = dmsState(will.dms);
  const banner = dmsMessage(dms);
  const executorProfile = will.executor ? executors.find((e) => e.id === will.executor.id) : null;
  const assetById = (assetId) => assets.find((a) => a.id === assetId);

  // Beneficiaries assigned to one will-asset (backend allows more than one).
  function beneficiariesFor(entry) {
    return (will.assignments || [])
      .filter((a) => a.willAssetId === entry.willAssetId)
      .map((a) => (will.beneficiaries || []).find((b) => b.id === a.beneficiaryDesignationId))
      .filter(Boolean);
  }

  function toggleAsset(key) {
    setOpenAssets((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

=======
    setAccessState("checking");
    setOpenAssets(new Set());

    async function checkWillAccess() {
      try {
        // Chi lay danh sach / summary
        const wills = await getWills(user?.id);

        if (version !== requestVersion.current) return;

        const summary = wills.find(
          (w) => String(w.id).toLowerCase() === String(id).toLowerCase()
        );

        if (!summary) {
          setLoadError("Will not found or access denied.");
          setAccessState("error");
          return;
        }

        // Draft phuc vu Create Will: khong can PIN
        if (summary.status === "DRAFT") {
          const [willData, assetsData] = await Promise.all([
            getWill(user?.id, id),
            getAssets(user?.id),
          ]);

          if (version !== requestVersion.current) return;

          setWill(willData);
          setAssets(assetsData);
          setAccessState("ready");

          loadExtraInformation(willData, version);
          return;
        }

        // Will da Submit: chua tai chi tiet
        // Phai nhap PIN truoc
        setAccessState("pin");
      } catch (err) {
        if (version !== requestVersion.current) return;

        setLoadError(
          err.message || "Could not check Will access."
        );
        setAccessState("error");
      }
    }

    checkWillAccess();

    return () => {
      ++requestVersion.current;
    };
  }, [id, user?.id]);

  // ==========================================
  // B2. PIN DUNG -> GOI API LAY WILL DETAIL
  // ==========================================
  async function handleWillPinVerified(grant) {
    const version = requestVersion.current;

    // Backend kiem tra Grant truoc khi tra Will Detail
    const [willData, assetsData] = await Promise.all([
      getWill(user?.id, id, grant),
      getAssets(user?.id),
    ]);

    if (version !== requestVersion.current) return;

    setPinGrant(grant);
    setWill(willData);
    setAssets(assetsData);

    // Chi mo trang sau khi Backend tra chi tiet thanh cong
    setAccessState("ready");
    setLoadError("");

    loadExtraInformation(willData, version);
  }

  // ==========================================
  // B3. TAI THONG TIN PHU SAU KHI DUOC PHEP
  // ==========================================
  function loadExtraInformation(willData, version) {
    getExecutors()
      .then((data) => {
        if (version === requestVersion.current) {
          setExecutors(data);
        }
      })
      .catch(() => {
        if (version === requestVersion.current) {
          setExecutors([]);
        }
      });

    if (willData.status !== "DRAFT") {
      getHandover(id)
        .then((data) => {
          if (version === requestVersion.current) {
            setHandover(data);
          }
        })
        .catch(() => {
          if (version === requestVersion.current) {
            setHandover(null);
          }
        });
    }
  }

  function loadHandover() {
    const version = requestVersion.current;

    getHandover(id)
      .then((data) => {
        if (version === requestVersion.current) {
          setHandover(data);
        }
      })
      .catch(() => {
        if (version === requestVersion.current) {
          setHandover(null);
        }
      });
  }

  // ==========================================
  // EXECUTOR REVIEW
  // ==========================================
  async function handleSubmitReview(e) {
    e.preventDefault();

    setReview((r) => ({
      ...r,
      busy: true,
      error: "",
    }));

    try {
      await createExecutorReview(will.executor.id, {
        willId: will.id,
        rating: review.rating,
        comment: review.comment,
      });

      setReview((r) => ({
        ...r,
        busy: false,
        done: true,
      }));
    } catch (err) {
      setReview((r) => ({
        ...r,
        busy: false,
        error: err.message || "Could not submit review",
      }));
    }
  }

  // ==========================================
  // DMS CHECK-IN
  // ==========================================
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx
  async function handleCheckIn() {
    setCheckInBusy(true);
    setCheckInError("");
    setCheckInDone("");

    try {
      const updated = await checkInDms(
        user?.id,
        will.id,
        pinGrant
      );

      setWill(updated);

      setCheckInDone(
        updated.dms?.nextCheckInAt
          ? `Check-in recorded. Your next check-in is due on ${fmtDate(
              updated.dms.nextCheckInAt
            )}.`
          : "Check-in recorded."
      );
    } catch (err) {
      setCheckInError(
        err?.status === 423
          ? "PIN access expired while refreshing. Return to My Wills and unlock again. Check your DMS status before repeating Check-in."
          : err.message || "Could not check in"
      );
    } finally {
      setCheckInBusy(false);
    }
  }

  async function handleCancelHandover() {
    setCheckInError("");

    try {
      setWill(
        await cancelDeathHandover(user?.id, will.id)
      );
    } catch (err) {
      setCheckInError(
        err.message || "Could not cancel handover"
      );
    }
  }

  // ==========================================
  // IMMEDIATE HANDOVER
  // ==========================================
  async function handleConfirmImmediateHandover() {
    setHandoverBusy(true);
    setHandoverError("");

    try {
      const updated = await requestImmediateHandover(
        user?.id,
        will.id,
        pinGrant
      );

      setWill(updated);
      setConfirmingHandover(false);
<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
=======

      loadHandover();
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx
    } catch (err) {
      setHandoverError(
        err?.status === 423
          ? "PIN access expired while refreshing. Return to My Wills and unlock again. Check handover status before resubmitting."
          : err.message || "Could not request immediate handover"
      );
    } finally {
      setHandoverBusy(false);
    }
  }

  // ==========================================
  // CHUA KIEM TRA XONG
  // ==========================================
  if (accessState === "checking") {
    return (
      <div className={styles.page}>
        <Link
          to="/dashboard/my-wills"
          className={styles.backLink}
        >
          ← Back to My Wills
        </Link>

        <p className={styles.muted}>
          Checking Will access...
        </p>
      </div>
    );
  }

  // ==========================================
  // WILL DA SUBMIT -> BAT BUOC NHAP PIN
  // ==========================================
  if (accessState === "pin") {
    return (
      <div className={styles.page}>
        <Link
          to="/dashboard/my-wills"
          className={styles.backLink}
        >
          ← Back to My Wills
        </Link>

        <h1 className={styles.title}>
          Will Details
        </h1>

        <p className={styles.muted}>
          Verify your 6-digit security PIN to view this Will.
        </p>

        <SecurityPinModal
          scope="WILL_DETAIL"
          resourceId={id}
          onVerified={handleWillPinVerified}
          onClose={() => navigate("/dashboard/my-wills")}
        />
      </div>
    );
  }

  // ==========================================
  // LOI TAI WILL
  // ==========================================
  if (accessState === "error") {
    return (
      <div className={styles.page}>
        <Link
          to="/dashboard/my-wills"
          className={styles.backLink}
        >
          ← Back to My Wills
        </Link>

        <p role="alert" className={styles.errorText}>
          {loadError || "Could not load Will details."}
        </p>
      </div>
    );
  }

  if (!will) {
    return (
      <p className={styles.muted}>
        Will not available.
      </p>
    );
  }

  // ==========================================
  // CHI TIET WILL SAU KHI DUOC PHEP
  // ==========================================
  const isDraft = will.status === "DRAFT";
  const dms = dmsState(will.dms);
  const banner = dmsMessage(dms);

  const executorProfile = will.executor
    ? executors.find((e) => e.id === will.executor.id)
    : null;

  const assetById = (assetId) =>
    assets.find((a) => a.id === assetId);

  function beneficiariesFor(entry) {
    return (will.assignments || [])
      .filter((a) => a.willAssetId === entry.willAssetId)
      .map((a) =>
        (will.beneficiaries || []).find(
          (b) => b.id === a.beneficiaryDesignationId
        )
      )
      .filter(Boolean);
  }

  function toggleAsset(key) {
    setOpenAssets((prev) => {
      const next = new Set(prev);

      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
  }

  return (
    <div className={styles.page}>
      <div className={styles.headRow}>
        <h1 className={styles.title}>
          {will.name}
        </h1>

        <span
          className={`${styles.badge} ${
            styles[will.status] || ""
          }`}
        >
          {will.status.replaceAll("_", " ")}
        </span>
      </div>

      <Link
        to="/dashboard/my-wills"
        className={styles.backLink}
      >
        ← Back to My Wills
      </Link>

      {/* DMS BANNER */}
      {banner && (
        <div
          className={
            banner.tone === "danger"
              ? styles.bannerDanger
              : styles.bannerWarn
          }
          role="alert"
        >
          {banner.text}{" "}

          <button
            className={styles.linkBtn}
            disabled={checkInBusy}
            onClick={handleCheckIn}
          >
            Check in now
          </button>
        </div>
      )}

      {/* DRAFT */}
      {isDraft && (
        <div className={styles.pendingNote}>
          This will is still a draft.{" "}
          <Link
            to={`/dashboard/create-will?willId=${will.id}`}
            className={styles.linkBtn}
          >
            Continue editing
          </Link>
        </div>
      )}

      {/* EXECUTOR PENDING */}
      {will.status === "PENDING_EXECUTOR" &&
        will.executor?.status === "PENDING" && (
          <div className={styles.pendingNote}>
            Waiting for{" "}
            {will.executor?.name || "the assigned executor"}{" "}
            to accept the executor request.
          </div>
        )}

      {/* EXECUTOR DECLINED */}
      {will.status === "PENDING_EXECUTOR" &&
        will.executor?.status === "DECLINED" && (
          <div className={styles.declineNote}>
            {will.executor?.name || "The executor"}{" "}
            declined this will. You can delete it from
            My Wills and create a new one.
          </div>
        )}

      {/* IMMEDIATE HANDOVER */}
      {will.status === "ACTIVE" && (
        <div className={styles.demoRow}>
          <button
            className={styles.secondaryBtn}
            onClick={() => setConfirmingHandover(true)}
          >
            Immediate Handover
          </button>
        </div>
      )}

      {confirmingHandover && (
<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
        <div className={styles.overlay} onClick={() => !handoverBusy && setConfirmingHandover(false)}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Are you sure?</h2>
=======
        <div
          className={styles.overlay}
          {...handoverBackdrop}
        >
          <div
            className={styles.modal}
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className={styles.modalTitle}>
              Are you sure?
            </h2>

>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx
            <p className={styles.itemMeta}>
              This will send "{will.name}" to{" "}
              {will.executor?.name || "your executor"}{" "}
              to hand its assets over now, while
              you're still alive.
            </p>

            {handoverError && (
              <p className={styles.errorText}>
                {handoverError}
              </p>
            )}

            <div className={styles.actions}>
              <button
                className={styles.secondaryBtn}
                disabled={handoverBusy}
                onClick={() =>
                  setConfirmingHandover(false)
                }
              >
                Cancel
              </button>

              <button
                className={styles.primaryBtn}
                disabled={handoverBusy}
                onClick={handleConfirmImmediateHandover}
              >
                {handoverBusy
                  ? "Sending..."
                  : "Yes, Send to Executor"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* OVERVIEW */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Overview
        </h2>

        <Row label="Will Name" value={will.name} />
        <Row
          label="Description"
          value={will.description || "—"}
        />
        <Row
          label="Status"
          value={will.status.replaceAll("_", " ")}
        />
        <Row
          label="Created Date"
          value={fmtDate(will.createdAt)}
        />

        {will.submittedAt && (
          <Row
            label="Submitted"
            value={fmtDate(will.submittedAt)}
          />
        )}

        {will.activatedAt && (
          <Row
            label="Activated"
            value={fmtDate(will.activatedAt)}
          />
        )}

        <Row
          label="Assets"
          value={(will.assets || []).length}
        />
        <Row
          label="Beneficiaries"
          value={(will.beneficiaries || []).length}
        />
      </section>

      {/* WILL ASSETS */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Assets
        </h2>

        {(will.assets || []).length === 0 && (
          <p className={styles.itemMeta}>
            No assets in this will.
          </p>
        )}

        {(will.assets || []).map((entry) => {
          const key =
            entry.willAssetId || entry.assetId;

          const full = assetById(entry.assetId);
          const recipients = beneficiariesFor(entry);
          const open = openAssets.has(key);

          return (
            <div
              key={key}
              className={styles.assetBlock}
            >
              <button
                type="button"
                className={styles.assetHead}
                aria-expanded={open}
                onClick={() => toggleAsset(key)}
              >
                <div>
                  <p className={styles.itemTitle}>
                    {full?.name ||
                      entry.assetName ||
                      "Asset"}
                  </p>

                  <p className={styles.itemMeta}>
                    {full?.type || entry.assetType}

                    {full?.type === "Online Account" &&
                    full.accountType
                      ? ` · ${providerLabel(full.accountType)}`
                      : ""}

                    {full
                      ? ` · ${verificationInfo(full).label}`
                      : ""}
                  </p>
                </div>

                <div>
                  <p className={styles.itemMeta}>
                    Assigned To
                  </p>

                  <p className={styles.itemTitle}>
                    {recipients
                      .map((b) => b.fullName)
                      .join(", ") || "—"}
                  </p>
                </div>

                <span className={styles.chev}>
                  {open ? "▲ Hide" : "▼ Details"}
                </span>
              </button>

              {open && (
                <div className={styles.assetBody}>
                  <p className={styles.itemTitle}>
                    Recipients
                  </p>

                  {recipients.length === 0 && (
                    <p className={styles.itemMeta}>
                      No recipient assigned.
                    </p>
                  )}

                  {recipients.map((b) => (
                    <p
                      key={b.id}
                      className={styles.itemMeta}
                    >
                      {b.fullName} · {b.email}
                      {b.phone ? ` · ${b.phone}` : ""}
                      {" · "}
                      {b.relationship ||
                        "Relationship not set"}
                      {" · "}
                      {b.beneficiaryAccountId
                        ? "Has a LegacyVault account"
                        : "No account yet"}
                    </p>
                  ))}

                  <p
                    className={styles.itemTitle}
                    style={{ marginTop: 10 }}
                  >
                    Executor
                  </p>

                  <p className={styles.itemMeta}>
                    {will.executor
                      ? `${
                          will.executor.name
                        } · ${
                          EXECUTOR_STATUS_LABEL[
                            will.executor.status
                          ] || will.executor.status
                        }`
                      : "No executor assigned."}
                  </p>
<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
=======

                  {specialistFor(
                    will,
                    entry.willAssetId
                  ) && (
                    <p className={styles.itemMeta}>
                      Suggested specialist:{" "}
                      {
                        specialistFor(
                          will,
                          entry.willAssetId
                        ).name
                      }{" "}
                      (recommendation only, does not
                      accept or get paid)
                    </p>
                  )}
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx
                </div>
              )}
            </div>
          );
        })}

        <p className={styles.hintSmall}>
          Assets are locked to this will once
          submitted; add new assets in a new will.
        </p>
      </section>

      {/* BENEFICIARIES */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Beneficiaries
        </h2>

        {(will.beneficiaries || []).length === 0 && (
          <p className={styles.itemMeta}>
            No beneficiaries yet.
          </p>
        )}

        {(will.beneficiaries || []).map((b) => {
          const count = (
            will.assignments || []
          ).filter(
            (a) =>
              a.beneficiaryDesignationId === b.id
          ).length;

          return (
            <div
              key={b.id}
              className={styles.assetRow}
            >
              <div>
                <p className={styles.itemTitle}>
                  {b.fullName}
                </p>

                <p className={styles.itemMeta}>
                  {b.email}
                  {b.phone ? ` · ${b.phone}` : ""}
                </p>

                <p className={styles.itemMeta}>
                  Relationship:{" "}
                  {b.relationship || "—"}
                  {" · "}
                  {b.beneficiaryAccountId
                    ? "Has a LegacyVault account"
                    : "No account yet"}
                </p>
              </div>

              <div>
                <p className={styles.itemMeta}>
                  Status
                </p>

                <p className={styles.itemTitle}>
                  {(b.status || "").replaceAll(
                    "_",
                    " "
                  ) || "—"}
                </p>

                <p className={styles.itemMeta}>
                  {count} asset
                  {count === 1 ? "" : "s"}
                </p>
              </div>
            </div>
          );
        })}
      </section>

      {/* EXECUTOR */}
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Executor
        </h2>

        {will.executor ? (
          <>
            <Row
              label="Name"
              value={will.executor.name || "—"}
            />
            <Row
              label="Assignment Status"
              value={
                EXECUTOR_STATUS_LABEL[
                  will.executor.status
                ] || will.executor.status
              }
            />

            {executorProfile && (
              <>
                <Row
                  label="Introduction"
                  value={
                    executorProfile.specialization ||
                    "—"
                  }
                />
                <Row
                  label="Experience"
                  value={`${executorProfile.experienceYears} years`}
                />
                <Row
                  label="Completed Cases"
                  value={
                    executorProfile.completedCases
                  }
                />
                <Row
                  label="Rating"
                  value={`⭐ ${Number(
                    executorProfile.rating || 0
                  ).toFixed(1)}`}
                />
                <ExecutorFeeInsight executor={executorProfile} />
              </>
            )}
          </>
        ) : (
          <p className={styles.itemMeta}>
            No executor assigned.
          </p>
        )}
      </section>

<<<<<<< Updated upstream:frontend/src/pages/WillDetails/WillDetails.jsx
      {/* DMS */}
=======
      {/* HANDOVER PROGRESS */}
      {handover && (
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>
            Handover
          </h2>

          <Row
            label="Type"
            value={handover.type}
          />
          <Row
            label="Status"
            value={String(
              handover.status
            ).replaceAll("_", " ")}
          />
          <Row
            label="Started"
            value={fmtDate(
              handover.startedAt,
              true
            )}
          />
          <Row
            label="Progress"
            value={`${handover.deliveredItems} / ${handover.totalItems} delivered`}
          />

          {(handover.items || []).map((it) => (
            <div
              key={it.id}
              className={styles.assetRow}
            >
              <div>
                <div className={styles.itemTitle}>
                  {it.assetName} →{" "}
                  {it.beneficiaryName}

                  {it.relationship
                    ? ` (${it.relationship})`
                    : ""}
                </div>

                <div className={styles.itemMeta}>
                  {it.status}

                  {it.deliveredAt
                    ? ` · ${fmtDate(
                        it.deliveredAt,
                        true
                      )}`
                    : ""}
                </div>
              </div>
            </div>
          ))}
        </section>
      )}

      {/* EXECUTOR REVIEW */}
      {will.status === "HANDED_OVER" &&
        will.executor && (
          <section className={styles.section}>
            <h2 className={styles.sectionTitle}>
              Review your executor
            </h2>

            {review.done ? (
              <p
                role="status"
                className={styles.successText}
              >
                Thanks, your review was saved.
              </p>
            ) : (
              <form
                className={styles.editForm}
                onSubmit={handleSubmitReview}
              >
                <label className={styles.label}>
                  Rating

                  <select
                    className={styles.input}
                    value={review.rating}
                    onChange={(e) =>
                      setReview((r) => ({
                        ...r,
                        rating: Number(
                          e.target.value
                        ),
                      }))
                    }
                  >
                    {[5, 4, 3, 2, 1].map(
                      (n) => (
                        <option
                          key={n}
                          value={n}
                        >
                          {"⭐".repeat(n)}
                        </option>
                      )
                    )}
                  </select>
                </label>

                <label className={styles.label}>
                  Comment

                  <textarea
                    className={styles.textarea}
                    maxLength={1000}
                    value={review.comment}
                    onChange={(e) =>
                      setReview((r) => ({
                        ...r,
                        comment:
                          e.target.value,
                      }))
                    }
                  />
                </label>

                {review.error && (
                  <p
                    role="alert"
                    className={styles.errorText}
                  >
                    {review.error}
                  </p>
                )}

                <div className={styles.actions}>
                  <button
                    className={styles.primaryBtn}
                    disabled={review.busy}
                  >
                    {review.busy
                      ? "Saving..."
                      : "Submit Review"}
                  </button>
                </div>
              </form>
            )}
          </section>
        )}

      {/* DEAD MAN'S SWITCH */}
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/pages/WillDetails/WillDetails.jsx
      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>
          Dead Man's Switch
        </h2>

        {!will.dms ? (
          <p className={styles.itemMeta}>
            Not configured yet.
          </p>
        ) : (
          <>
            <Row
              label="Status"
              value={
                will.dms.enabled
                  ? will.dms.status
                  : "Disabled"
              }
            />

            <Row
              label="Check-in Interval"
              value={`Every ${will.dms.checkInDays} day${
                will.dms.checkInDays === 1
                  ? ""
                  : "s"
              }`}
            />

            <Row
              label="Reminder Before Due Date"
              value={`${will.dms.reminderDays} day${
                will.dms.reminderDays === 1
                  ? ""
                  : "s"
              }`}
            />

            <Row
              label="Maximum Missed Check-ins"
              value={will.dms.maxMissed}
            />

            <Row
              label="Missed Check-ins"
              value={`${
                will.dms.missedCount ?? 0
              } / ${will.dms.maxMissed}`}
            />

            <Row
              label="Last Check-in"
              value={fmtDate(
                will.dms.lastCheckIn
              )}
            />

            <Row
              label="Next Check-in Due"
              value={fmtDate(
                will.dms.nextCheckInAt
              )}
            />

            {(will.dms.missedCount ?? 0) >
              0 && (
              <div
                className={
                  styles.bannerDanger
                }
              >
                Miss{" "}
                {will.dms.missedCount} of{" "}
                {will.dms.maxMissed}.
                When the maximum is
                reached your executor
                is asked to verify.
              </div>
            )}

            {will.status ===
              "PENDING_VERIFICATION" && (
              <div
                className={
                  styles.pendingNote
                }
              >
                Your Dead Man's Switch
                has triggered and a
                handover case is open
                with your Executor.

                <div className={styles.demoRow}>
                  <button
                    className={styles.primaryBtn}
                    onClick={
                      handleCancelHandover
                    }
                  >
                    I'm Still Active /
                    Cancel Handover
                  </button>
                </div>
              </div>
            )}

            {checkInError && (
              <p
                role="alert"
                className={
                  styles.errorText
                }
              >
                {checkInError}
              </p>
            )}

            {checkInDone && (
              <p
                role="status"
                className={
                  styles.successText
                }
              >
                {checkInDone}
              </p>
            )}

            <div className={styles.actions}>
              <button
                className={styles.primaryBtn}
                onClick={handleCheckIn}
                disabled={
                  checkInBusy ||
                  !will.dms.enabled ||
                  !dms.active ||
                  will.status ===
                    "PENDING_VERIFICATION"
                }
                title={
                  !dms.active
                    ? "Check-in is available once the will is submitted and the DMS is active."
                    : undefined
                }
              >
                {checkInBusy
                  ? "Checking in..."
                  : "Check In Now"}
              </button>
            </div>
          </>
        )}
      </section>
    </div>
  );
}

function Row({ label, value }) {
  return (
    <div className={styles.row}>
      <span className={styles.rowLabel}>
        {label}
      </span>
      <span className={styles.rowValue}>
        {value}
      </span>
    </div>
  );
}
