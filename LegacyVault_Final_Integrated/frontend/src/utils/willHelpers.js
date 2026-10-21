export const ASSET_LOCK_STATUSES = "ANY";

function locks(status) {
  return ASSET_LOCK_STATUSES === "ANY" || ASSET_LOCK_STATUSES.includes(status);
}

export function buildAssetLocks(wills, exceptWillId = null) {
  const map = new Map();
  (wills || []).forEach((w) => {
    if (!w || w.id === exceptWillId || !Array.isArray(w.assets) || !locks(w.status)) return;
    w.assets.forEach((a) => {
      if (!map.has(a.assetId)) map.set(a.assetId, { willId: w.id, willName: w.name || w.title || "Untitled will" });
    });
  });
  return map;
}

export const RELATIONSHIPS = ["Daughter", "Son", "Spouse", "Parent", "Sibling", "Friend", "Other"];

export const BENEFICIARY_STATUS_LABEL = {
  DESIGNATED: "Designated",
};

export function fmtDate(iso, withTime = false) {
  if (!iso) return "—";
  const d = new Date(iso);
  return withTime ? d.toLocaleString("en-US") : d.toLocaleDateString("en-US");
}

export function dmsState(dms) {
  if (!dms || !dms.enabled) return { enabled: false };
  const next = dms.nextCheckInAt ? new Date(dms.nextCheckInAt) : null;
  const daysLeft = next ? Math.ceil((next.getTime() - Date.now()) / 86400000) : null;
  const overdue = next ? next.getTime() < Date.now() : false;
  const inReminderWindow = daysLeft != null && !overdue && daysLeft <= (dms.reminderDays ?? 0);
  const missed = dms.missedCount ?? 0;
  const max = dms.maxMissed ?? 0;
  return {
    enabled: true,
    active: String(dms.status).toUpperCase() === "ACTIVE",
    next,
    daysLeft,
    overdue,
    inReminderWindow,
    missed,
    max,
    due: overdue || inReminderWindow,
  };
}

export function dmsMessage(state) {
  if (!state.enabled || !state.active) return null;
  if (state.missed > 0) {
    return {
      tone: "danger",
      text: `Missed check-in ${state.missed}${state.max ? ` of ${state.max}` : ""}. Please check in now.`,
    };
  }
  if (state.overdue) return { tone: "danger", text: "Your check-in is overdue. Please check in now." };
  if (state.inReminderWindow) {
    return {
      tone: "warn",
      text: state.daysLeft <= 0 ? "Your check-in is due today." : `Your next check-in is due in ${state.daysLeft} day${state.daysLeft === 1 ? "" : "s"}.`,
    };
  }
  return null;
}

export const INTERVAL_PRESETS = [1, 7, 14, 30, 60, 90, 180, 365];

export function validateDms({ checkInDays, reminderDays, maxMissed }) {
  if (!Number.isInteger(checkInDays) || checkInDays < 1 || checkInDays > 3650) return "Check-in interval must be between 1 and 3650 days.";
  if (!Number.isInteger(reminderDays) || reminderDays < 0) return "Reminder days cannot be negative.";
  if (reminderDays >= checkInDays) return "Reminder must be fewer days than the check-in interval.";
  if (!Number.isInteger(maxMissed) || maxMissed < 1 || maxMissed > 100) return "Maximum missed check-ins must be between 1 and 100.";
  return "";
}
