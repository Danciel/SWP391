import { ApiError } from "./client";
import {
  createDmsTrigger,
  getRegisteredUserById,
  findRegisteredUserIdByEmail,
  cancelCaseByWillId,
  createHandoverRequest,
  createExecutorHireRequest,
} from "./mockBackend";

// ---------------------------------------------------------------------------
// Mock "vault" backend: profile/identity, assets, wills, beneficiaries,
// executors, and the beneficiary claim flow. Same pattern as mockBackend.js
// (localStorage as the fake DB). Swap for real API calls later.
// ---------------------------------------------------------------------------

const PROFILE_KEY = "lv_mock_profile";
const ASSETS_KEY = "lv_mock_assets";
const WILLS_KEY = "lv_mock_wills";
const INHERITED_KEY = "lv_mock_inherited";
const NETWORK_DELAY_MS = 450;

function daysToMs(days) {
  return (Number(days) || 0) * 24 * 60 * 60 * 1000;
}

function delay(ms = NETWORK_DELAY_MS) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function readJSON(key, fallback) {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch {
    return fallback;
  }
}

function writeJSON(key, value) {
  localStorage.setItem(key, JSON.stringify(value));
}

function randomId(prefix = "id") {
  return `${prefix}_${Math.random().toString(36).slice(2, 10)}`;
}

function scopedKey(base, userId) {
  return `${base}_${userId || "anon"}`;
}

// ---------------------------------------------------------------------------
// Executors directory (static, read-only — Executor onboarding is out of scope)
// ---------------------------------------------------------------------------
export const EXECUTORS = [
  {
    id: "exec_david",
    name: "David Nguyen",
    specialization: "Digital Estate Specialist",
    license: "LAW-2026-00182",
    rating: 4.9,
    reviews: 128,
    experienceYears: 8,
    completedCases: 86,
    fee: 3000000,
    available: true,
  },
  {
    id: "exec_linh",
    name: "Linh Tran",
    specialization: "Crypto Asset Specialist",
    license: "LAW-2026-00219",
    rating: 4.8,
    reviews: 94,
    experienceYears: 6,
    completedCases: 61,
    fee: 2500000,
    available: true,
  },
  {
    id: "exec_khoa",
    name: "Khoa Pham",
    specialization: "Family Legacy Attorney",
    license: "LAW-2025-00087",
    rating: 4.7,
    reviews: 152,
    experienceYears: 11,
    completedCases: 140,
    fee: 3500000,
    available: true,
  },
];

// A tiny directory used by "search beneficiary by email/phone" so the Assign
// Beneficiaries step can show a result for an existing account.
const BENEFICIARY_DIRECTORY = [
  { fullName: "Sarah Nguyen", email: "sarah@gmail.com", phone: "0901234567", identityNumber: "079201012345" },
  { fullName: "Michael Tran", email: "michael@gmail.com", phone: "0912345678", identityNumber: "079202054321" },
];

// ---------------------------------------------------------------------------
// Profile / Identity Verification
// ---------------------------------------------------------------------------
function defaultProfile(user) {
  return {
    fullName: user?.fullName || "John Nguyen",
    email: user?.email || "john@gmail.com",
    emailVerified: true,
    phone: user?.phoneNumber || "",
    phoneVerified: false,
    identityNumber: "",
    identityStatus: "NOT_VERIFIED", // NOT_VERIFIED | VERIFIED
    faceVerified: false,
  };
}

export async function getProfile(userId, user) {
  await delay(300);
  const key = scopedKey(PROFILE_KEY, userId);
  let profile = readJSON(key, null);
  if (!profile) {
    profile = defaultProfile(user);
    writeJSON(key, profile);
  }
  return profile;
}

function saveProfile(userId, patch) {
  const key = scopedKey(PROFILE_KEY, userId);
  const current = readJSON(key, defaultProfile());
  const next = { ...current, ...patch };
  writeJSON(key, next);
  return next;
}

export async function sendPhoneOtp(userId, phone) {
  await delay(500);
  if (!phone || phone.replace(/\D/g, "").length < 9) {
    throw new ApiError("Please enter a valid phone number", 400);
  }
  saveProfile(userId, { phone });
  // eslint-disable-next-line no-console
  console.info(`%c[mock] Phone OTP sent to ${phone}: 123456`, "color:#4f46e5;font-weight:bold;");
  return { sent: true };
}

export async function verifyPhoneOtp(userId, phone, otp) {
  await delay(500);
  if (otp !== "123456" && otp.length !== 6) {
    throw new ApiError("Invalid verification code", 400);
  }
  return saveProfile(userId, { phone, phoneVerified: true });
}

export async function verifyIdentityNumber(userId, identityNumber) {
  await delay(500);
  if (!identityNumber || identityNumber.replace(/\D/g, "").length < 9) {
    throw new ApiError("Please enter a valid identity number", 400);
  }
  return saveProfile(userId, { identityNumber });
}

export async function verifyFace(userId) {
  // Simulates: face capture -> third-party identity provider check
  await delay(1400);
  return saveProfile(userId, { faceVerified: true, identityStatus: "VERIFIED" });
}

// ---------------------------------------------------------------------------
// Assets
// ---------------------------------------------------------------------------
export async function getAssets(userId) {
  await delay(300);
  return readJSON(scopedKey(ASSETS_KEY, userId), []);
}

export async function addAsset(userId, { name, type, description }) {
  await delay(500);
  if (!name) throw new ApiError("Asset name is required", 400);
  const key = scopedKey(ASSETS_KEY, userId);
  const assets = readJSON(key, []);
  const asset = {
    id: randomId("asset"),
    name,
    type: type || "Document",
    description: description || "",
    status: "Secured",
    createdAt: new Date().toISOString(),
  };
  assets.push(asset);
  writeJSON(key, assets);
  return asset;
}

export async function updateAsset(userId, assetId, { name, type, description }) {
  await delay(500);
  if (!name) throw new ApiError("Asset name is required", 400);
  const key = scopedKey(ASSETS_KEY, userId);
  const assets = readJSON(key, []);
  const idx = assets.findIndex((a) => a.id === assetId);
  if (idx === -1) throw new ApiError("Asset not found", 404);
  assets[idx] = {
    ...assets[idx],
    name,
    type: type || assets[idx].type,
    description: description ?? assets[idx].description,
    updatedAt: new Date().toISOString(),
  };
  writeJSON(key, assets);
  return assets[idx];
}

export async function uploadAssetFile(userId, assetId, file) {
  await delay(700);

  if (!file) {
    throw new ApiError("Please select a file", 400);
  }

  const key = scopedKey(ASSETS_KEY, userId);
  const assets = readJSON(key, []);

  const idx = assets.findIndex((a) => a.id === assetId);

  if (idx === -1) {
    throw new ApiError("Asset not found", 404);
  }

  // Mock upload: store file metadata only.
  const fileInfo = {
    id: randomId("file"),
    name: file.name,
    size: file.size,
    type: file.type || "application/octet-stream",
    uploadedAt: new Date().toISOString(),
    secured: true,
  };

  assets[idx] = {
    ...assets[idx],
    file: fileInfo,
    updatedAt: new Date().toISOString(),
  };

  writeJSON(key, assets);

  return assets[idx];
}

// ---------------------------------------------------------------------------
// Wills
// ---------------------------------------------------------------------------
function getWillsRaw(userId) {
  return readJSON(scopedKey(WILLS_KEY, userId), []);
}

function saveWillsRaw(userId, wills) {
  writeJSON(scopedKey(WILLS_KEY, userId), wills);
}

// ---------------------------------------------------------------------------
// Dead Man's Switch auto check-in: `missedCount` is derived from how long
// it's been since the last check-in vs. the configured check-in interval.
// Once missedCount reaches maxMissed, the will auto-transitions to
// PENDING_VERIFICATION and a real Dead Man's Switch trigger is opened on the
// Executor side (mockBackend.js), carrying the real will id and owner name
// instead of relying on the hardcoded demo seed.
// ---------------------------------------------------------------------------
function applyDmsAutoCheck(userId, will) {
  if (!will.dms?.enabled) return false;
  // Only a will that's actually being monitored can trigger (or keep
  // counting missed check-ins while a case is already open).
  if (will.status !== "ACTIVE" && will.status !== "PENDING_VERIFICATION") return false;

  const checkInMs = daysToMs(will.dms.checkInDays || 30);
  if (checkInMs <= 0) return false;
  const baseline = will.dms.lastCheckIn || will.updatedAt || will.createdAt;
  const elapsed = Date.now() - new Date(baseline).getTime();
  const missedCount = Math.max(0, Math.floor(elapsed / checkInMs));
  const maxMissed = will.dms.maxMissed ?? 2;

  let changed = false;
  if (will.dms.missedCount !== missedCount) {
    will.dms.missedCount = missedCount;
    changed = true;
  }

  if (missedCount >= maxMissed && will.status === "ACTIVE") {
    will.status = "PENDING_VERIFICATION";
    will.dms.status = "TRIGGERED";
    will.dms.triggeredAt = new Date().toISOString();
    changed = true;

    const owner = getRegisteredUserById(userId);
    const ownerName = owner?.fullName || will.name || "Vault Owner";
    const trigger = createDmsTrigger({ willId: will.id, ownerId: userId, ownerName });
    will.dmsTriggerId = trigger?.id || will.dmsTriggerId || null;
  }

  return changed;
}

function getWillsWithAutoCheck(userId) {
  const wills = getWillsRaw(userId);
  let changed = false;
  wills.forEach((will) => {
    if (applyDmsAutoCheck(userId, will)) changed = true;
  });
  if (changed) saveWillsRaw(userId, wills);
  return wills;
}

export async function getWills(userId) {
  await delay(350);
  return getWillsWithAutoCheck(userId);
}

export async function getWill(userId, willId) {
  await delay(300);
  const will = getWillsWithAutoCheck(userId).find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  return will;
}

// Synchronous, non-delayed readers for cross-module use (called internally
// by mockBackend.js, e.g. when the Legal Verifier approves a case and needs
// to look up the real will/assets to deliver inheritance records).
export function getWillSync(userId, willId) {
  return getWillsRaw(userId).find((w) => w.id === willId) || null;
}

export function getAssetsSync(userId) {
  return readJSON(scopedKey(ASSETS_KEY, userId), []);
}

export async function saveWillDraft(userId, willId, patch) {
  await delay(400);
  const wills = getWillsRaw(userId);
  let will = wills.find((w) => w.id === willId);
  if (!will) {
    will = {
      id: willId || randomId("will"),
      name: "",
      description: "",
      status: "DRAFT",
      assets: [], // [{assetId, beneficiary: {fullName,email,phone,relationship,accountId|null}}]
      executor: null,
      // Dead Man's Switch is mandatory and defaults to ON as soon as a will
      // exists — the owner cannot turn it off, so `enabled` is never exposed
      // as a user-toggleable field anywhere in the UI.
      dms: { enabled: true, checkInDays: 30, reminderDays: 3, maxMissed: 2, status: "WAITING" },
      payment: { status: "NONE", fee: 0 },
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    wills.push(will);
  }
  Object.assign(will, patch, { updatedAt: new Date().toISOString() });
  // Guard against enabled ever being flipped off via a patch — DMS stays on.
  will.dms = { ...will.dms, enabled: true };
  saveWillsRaw(userId, wills);
  return will;
}

export async function deleteWillDraft(userId, willId) {
  await delay(300);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new Error("Will not found");
  if (will.status !== "DRAFT") {
    throw new Error("Only draft wills can be deleted");
  }
  const remaining = wills.filter((w) => w.id !== willId);
  saveWillsRaw(userId, remaining);
  return { success: true };
}

// ---------------------------------------------------------------------------
// Executor hiring: sends a real request to the Executor (mockBackend.js)
// carrying an asset snapshot, so the Executor Dashboard shows real
// name/type/beneficiary details instead of only the hardcoded demo seed.
// Same 7-day response deadline as other requests sent to the Executor.
// ---------------------------------------------------------------------------
function sendExecutorHireRequest(userId, will) {
  const assignedAssets = (will.assets || []).filter((a) => a.beneficiary);
  const ownerAssets = getAssetsSync(userId);
  const assetSnapshot = assignedAssets.map((entry) => {
    const asset = ownerAssets.find((a) => a.id === entry.assetId);
    return {
      id: entry.assetId,
      name: asset?.name || "Asset",
      type: asset?.type || "Document",
      description: asset?.description || "",
      status: asset?.status || "Secured",
      beneficiaryName: entry.beneficiary?.fullName || "—",
      beneficiaryEmail: entry.beneficiary?.email || "",
      beneficiaryPhone: entry.beneficiary?.phone || "",
      beneficiaryRelationship: entry.beneficiary?.relationship || "",
    };
  });

  const owner = getRegisteredUserById(userId);
  const ownerName = owner?.fullName || "Vault Owner";
  const requestedAt = new Date().toISOString();
  const deadlineAt = new Date(Date.now() + daysToMs(7)).toISOString();

  const request = createExecutorHireRequest({
    willId: will.id,
    ownerId: userId,
    ownerName,
    ownerEmail: owner?.email || "",
    ownerPhone: owner?.phoneNumber || "",
    willTitle: will.name,
    description: will.description || "",
    executorName: will.executor?.name,
    assetsCount: assignedAssets.length,
    beneficiariesCount: new Set(assignedAssets.map((a) => a.beneficiary?.email || a.beneficiary?.fullName)).size,
    assets: assetSnapshot,
    dmsStatus: will.dms?.enabled ? `Active · check-in every ${will.dms.checkInDays} days` : "Disabled",
    proposedFee: will.executor?.fee || 0,
    createdAt: will.createdAt,
    requestedAt,
    deadlineAt,
  });

  will.hireRequest = { requestId: request.id, requestedAt, deadlineAt };
}

export async function submitWill(userId, willId) {
  await delay(400);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  will.status = "PENDING_EXECUTOR_ACCEPTANCE";
  will.payment = { status: "HELD", fee: will.executor?.fee || 0 };
  sendExecutorHireRequest(userId, will);
  saveWillsRaw(userId, wills);
  return will;
}

// Called by mockBackend.js once the Executor accepts/declines a hiring
// request, to fold the decision back into the Owner's own will record
// (mirrors resolveImmediateHandover below).
export function resolveExecutorHiring(ownerId, willId, accepted) {
  const wills = getWillsRaw(ownerId);
  const will = wills.find((w) => w.id === willId);
  if (!will) return null;
  will.payment = { ...(will.payment || {}), status: accepted ? "PAID" : "RELEASED" };
  will.status = accepted ? "ACTIVE" : "NEEDS_EXECUTOR";
  if (accepted) will.dms = { ...will.dms, status: "ACTIVE" };
  saveWillsRaw(ownerId, wills);
  return will;
}

export async function updateWillOverview(userId, willId, patch) {
  await delay(400);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  will.name = patch.name ?? will.name;
  will.description = patch.description ?? will.description;
  saveWillsRaw(userId, wills);
  return will;
}

export async function updateWillBeneficiary(userId, willId, assetId, beneficiary) {
  await delay(400);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  const entry = will.assets.find((a) => a.assetId === assetId);
  if (entry) entry.beneficiary = beneficiary;
  saveWillsRaw(userId, wills);
  return will;
}

export async function changeWillExecutor(userId, willId, executor) {
  await delay(500);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  will.executor = executor;
  if (will.status === "NEEDS_EXECUTOR") {
    will.status = "PENDING_EXECUTOR_ACCEPTANCE";
    will.payment = { status: "HELD", fee: executor.fee };
    sendExecutorHireRequest(userId, will);
  }
  saveWillsRaw(userId, wills);
  return will;
}

export async function updateWillDms(userId, willId, dmsPatch) {
  await delay(400);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  // Dead Man's Switch can never be disabled by the owner — always keep it on
  // no matter what a caller passes in dmsPatch.
  will.dms = { ...will.dms, ...dmsPatch, enabled: true };
  saveWillsRaw(userId, wills);
  return will;
}

export async function checkInDms(userId, willId) {
  await delay(400);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  will.dms.lastCheckIn = new Date().toISOString();
  will.dms.missedCount = 0;
  saveWillsRaw(userId, wills);
  return will;
}

// "I'm still active / Cancel handover" — lets the Owner call off an
// in-progress handover while the Executor/Verifier case is still open,
// resetting the Dead Man's Switch back to normal.
export async function cancelDeathHandover(userId, willId) {
  await delay(400);
  const wills = getWillsWithAutoCheck(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  if (will.status !== "PENDING_VERIFICATION") {
    throw new ApiError("There is no open handover case to cancel for this will", 400);
  }

  const cancelled = cancelCaseByWillId(willId);
  if (!cancelled) {
    throw new ApiError(
      "The handover case has already been reviewed and can no longer be cancelled",
      400
    );
  }

  will.status = "ACTIVE";
  will.dms = {
    ...will.dms,
    status: "ACTIVE",
    missedCount: 0,
    lastCheckIn: new Date().toISOString(),
    triggeredAt: null,
  };
  will.dmsTriggerId = null;
  saveWillsRaw(userId, wills);
  return will;
}

// ---------------------------------------------------------------------------
// Immediate Handover: lets an Owner hand a will's assets to their Executor
// right now, while still alive (separate from the Dead Man's Switch flow).
// Flow: Owner requests -> Executor is notified and confirms/declines whether
// the beneficiary(ies) exist/are valid -> Owner sees the outcome. The
// Executor has a 7-day response deadline, same as other requests sent to
// them (see mockBackend.js).
// ---------------------------------------------------------------------------
export async function requestImmediateHandover(userId, willId) {
  await delay(500);
  const wills = getWillsRaw(userId);
  const will = wills.find((w) => w.id === willId);
  if (!will) throw new ApiError("Will not found", 404);
  if (will.status !== "ACTIVE") {
    throw new ApiError("Only an active will with a confirmed executor can be handed over immediately", 400);
  }
  if (!will.executor) {
    throw new ApiError("Assign an executor to this will before requesting a handover", 400);
  }
  const assignedAssets = (will.assets || []).filter((a) => a.beneficiary);
  if (assignedAssets.length === 0) {
    throw new ApiError("Assign at least one beneficiary before requesting a handover", 400);
  }

  const ownerAssets = getAssetsSync(userId);
  const assetSnapshot = assignedAssets.map((entry) => {
    const asset = ownerAssets.find((a) => a.id === entry.assetId);
    return {
      id: entry.assetId,
      name: asset?.name || "Asset",
      type: asset?.type || "Document",
      beneficiaryName: entry.beneficiary?.fullName || "—",
    };
  });

  const owner = getRegisteredUserById(userId);
  const ownerName = owner?.fullName || will.name || "Vault Owner";
  const requestedAt = new Date().toISOString();
  const deadlineAt = new Date(Date.now() + daysToMs(7)).toISOString();

  const request = createHandoverRequest({
    willId: will.id,
    ownerId: userId,
    ownerName,
    willTitle: will.name,
    executorName: will.executor.name,
    assets: assetSnapshot,
    requestedAt,
    deadlineAt,
  });

  will.status = "PENDING_HANDOVER_REVIEW";
  will.handover = { requestId: request.id, requestedAt, deadlineAt, decision: null, respondedAt: null };
  saveWillsRaw(userId, wills);
  return will;
}

// Called by mockBackend.js once the Executor confirms/declines a handover
// request, to fold the decision back into the Owner's own will record.
export function resolveImmediateHandover(ownerId, willId, confirmed) {
  const wills = getWillsRaw(ownerId);
  const will = wills.find((w) => w.id === willId);
  if (!will) return null;
  will.handover = {
    ...(will.handover || {}),
    decision: confirmed ? "CONFIRMED" : "DECLINED",
    respondedAt: new Date().toISOString(),
  };
  will.status = confirmed ? "HANDED_OVER" : "ACTIVE";
  saveWillsRaw(ownerId, wills);
  return will;
}

export function searchBeneficiaryDirectory(query) {
  const q = String(query || "").trim().toLowerCase();
  if (!q) return null;
  const digits = q.replace(/\D/g, "");
  return (
    BENEFICIARY_DIRECTORY.find(
      (b) =>
        b.email.toLowerCase() === q ||
        b.phone.replace(/\D/g, "") === digits ||
        (b.identityNumber && digits.length >= 9 && b.identityNumber === digits)
    ) || null
  );
}

// ---------------------------------------------------------------------------
// Beneficiary side: inherited wills + claim flow
// Seeded with demo data (this is a UI prototype — cross-account inheritance
// wiring belongs to a real backend) so the claim journey is explorable.
// ---------------------------------------------------------------------------
function seedInherited() {
  return [
    {
      id: "inh_family_legacy",
      willName: "Family Legacy 2026",
      from: "Michael Nguyen",
      ownerLifeStatus: "DECEASED",
      legalVerification: "APPROVED",
      claimStatus: "READY_TO_CLAIM", // READY_TO_CLAIM | CLAIMED
      assets: [
        { id: "a1", name: "Property Document", type: "Document" },
        { id: "a2", name: "Binance Wallet", type: "Crypto Wallet" },
        { id: "a3", name: "Important Documents", type: "Document" },
      ],
      claimKey: null,
    },
    {
      id: "inh_personal_legacy",
      willName: "Personal Legacy",
      from: "Anna Tran",
      ownerLifeStatus: "DECEASED",
      legalVerification: "APPROVED",
      claimStatus: "CLAIMED",
      assets: [
        { id: "b1", name: "Family Photos Drive", type: "Document" },
        { id: "b2", name: "Savings Account Info", type: "Document" },
      ],
      claimKey: null,
    },
  ];
}

// Called by mockBackend.js when a Legal Verifier approves a case, to hand
// real assets/beneficiaries over to the beneficiary's own inherited-wills
// list (keyed by the beneficiary's own registered account id).
export function upsertInheritedRecord(beneficiaryUserId, record) {
  if (!beneficiaryUserId || !record?.id) return null;
  const key = scopedKey(INHERITED_KEY, beneficiaryUserId);
  const list = readJSON(key, []);
  const idx = list.findIndex((w) => w.id === record.id);
  if (idx >= 0) list[idx] = { ...list[idx], ...record };
  else list.push(record);
  writeJSON(key, list);
  return record;
}

export async function getInheritedWills(userId) {
  await delay(350);
  const key = scopedKey(INHERITED_KEY, userId);
  let data = readJSON(key, null);
  if (!data) {
    data = seedInherited();
    writeJSON(key, data);
  }
  return data;
}

export async function getInheritedWill(userId, id) {
  await delay(250);
  const list = await getInheritedWills(userId);
  const will = list.find((w) => w.id === id);
  if (!will) throw new ApiError("Case not found", 404);
  return will;
}

function saveInherited(userId, list) {
  writeJSON(scopedKey(INHERITED_KEY, userId), list);
}

export async function issueClaimKey(userId, id) {
  await delay(600);
  const key = scopedKey(INHERITED_KEY, userId);
  const list = readJSON(key, seedInherited());
  const will = list.find((w) => w.id === id);
  if (!will) throw new ApiError("Case not found", 404);
  const claimKey = `LV-${new Date().getFullYear() % 100}${String(
    Math.floor(Math.random() * 90 + 10)
  )}-${randomId().slice(3, 7).toUpperCase()}-${randomId().slice(3, 7).toUpperCase()}`;
  will.claimKey = { code: claimKey, expiresAt: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString() };
  saveInherited(userId, list);
  return will.claimKey;
}

export async function confirmClaim(userId, id, claimKey) {
  await delay(600);
  const key = scopedKey(INHERITED_KEY, userId);
  const list = readJSON(key, seedInherited());
  const will = list.find((w) => w.id === id);
  if (!will) throw new ApiError("Case not found", 404);
  if (!will.claimKey || will.claimKey.code !== claimKey) {
    throw new ApiError("Invalid or expired claim key", 400);
  }
  will.claimStatus = "CLAIMED";
  saveInherited(userId, list);
  return will;
}

// ---------------------------------------------------------------------------
// Owner Dashboard: composed from the Owner's real wills/assets instead of a
// static payload, so it reflects whatever the Owner actually created in
// Create Will / Assets.
// ---------------------------------------------------------------------------
export async function getOwnerDashboardData(userId) {
  await delay(400);
  const wills = getWillsWithAutoCheck(userId);
  const assets = readJSON(scopedKey(ASSETS_KEY, userId), []);

  // The will that best represents the vault's current state: an open
  // handover case takes priority, then an active will, then anything else.
  const primaryWill =
    wills.find((w) => w.status === "PENDING_VERIFICATION") ||
    wills.find((w) => w.status === "ACTIVE") ||
    wills.find((w) => w.status === "PENDING_EXECUTOR_ACCEPTANCE" || w.status === "NEEDS_EXECUTOR") ||
    wills[0] ||
    null;

  const vaultStatus =
    primaryWill?.status === "PENDING_VERIFICATION"
      ? "PENDING_VERIFICATION"
      : primaryWill?.status === "ACTIVE"
        ? "ACTIVE"
        : "LOCKED";

  const deadManSwitch = primaryWill?.dms?.enabled
    ? {
      enabled: true,
      willId: primaryWill.id,
      checkInCycleDays: primaryWill.dms.checkInDays,
      lastCheckIn: primaryWill.dms.lastCheckIn || null,
      nextCheckInDue: primaryWill.dms.lastCheckIn
        ? new Date(
          new Date(primaryWill.dms.lastCheckIn).getTime() + daysToMs(primaryWill.dms.checkInDays)
        ).toISOString()
        : null,
      missedCount: primaryWill.dms.missedCount || 0,
      maxMissed: primaryWill.dms.maxMissed,
      caseOpen: primaryWill.status === "PENDING_VERIFICATION",
    }
    : { enabled: false };

  const beneficiaryNameByAsset = new Map();
  wills.forEach((w) =>
    (w.assets || []).forEach((a) => {
      if (a.beneficiary) beneficiaryNameByAsset.set(a.assetId, a.beneficiary.fullName);
    })
  );
  const assetRows = assets.map((a) => ({
    id: a.id,
    name: a.name,
    type: a.type,
    beneficiary: beneficiaryNameByAsset.get(a.id) || "Unassigned",
  }));

  const executors = primaryWill?.executor
    ? [
      {
        id: primaryWill.executor.id || "executor",
        name: primaryWill.executor.name,
        status: primaryWill.status === "ACTIVE" ? "Confirmed" : "Pending",
      },
    ]
    : [];

  const beneficiaryCounts = new Map();
  (primaryWill?.assets || []).forEach((a) => {
    if (!a.beneficiary) return;
    const key = a.beneficiary.email || a.beneficiary.fullName;
    const cur = beneficiaryCounts.get(key) || { name: a.beneficiary.fullName, count: 0 };
    cur.count += 1;
    beneficiaryCounts.set(key, cur);
  });
  const beneficiaries = Array.from(beneficiaryCounts.entries()).map(([key, v]) => ({
    id: key,
    name: v.name,
    assetsAssigned: v.count,
  }));

  const recentActivity = [];
  if (primaryWill?.dms?.lastCheckIn) {
    recentActivity.push({
      id: "act_checkin",
      time: primaryWill.dms.lastCheckIn,
      action: "Checked in for Dead Man's Switch",
    });
  }
  if (primaryWill?.updatedAt) {
    recentActivity.push({
      id: "act_updated",
      time: primaryWill.updatedAt,
      action: `Updated will "${primaryWill.name}"`,
    });
  }
  if (primaryWill?.createdAt) {
    recentActivity.push({
      id: "act_created",
      time: primaryWill.createdAt,
      action: `Created will "${primaryWill.name}"`,
    });
  }
  recentActivity.sort((a, b) => new Date(b.time) - new Date(a.time));

  return { vaultStatus, deadManSwitch, assets: assetRows, executors, beneficiaries, recentActivity };
}
