import { connectAccountAsset, uploadDocumentAsset, checkDocumentIntegrity, downloadDocument } from "./assetConnectionApi";
import { request } from "./client";
import { unsupported } from "./unsupported";
const r = (path, method = "GET", body, headers) => request(path, { method, body, headers });
const TYPE_CODES = { "Online Account": "ACCOUNT", "Crypto Wallet": "DIGITAL_WALLET", "Bank Account": "BANK_ACCOUNT", "Cloud Storage": "CLOUD_STORAGE", "Document": "DOCUMENT", "Other": "OTHER" };
const TYPE_NAMES = Object.fromEntries(Object.entries(TYPE_CODES).map(([name, code]) => [code, name]));
const mapAsset = a => ({ ...a, type: TYPE_NAMES[a.assetType] || a.assetType, accountType: a.provider || "" });
const mapDms = d => d ? ({ ...d, checkInDays: d.checkInIntervalDays, reminderDays: d.reminderBeforeDays, maxMissed: d.maxMissedCheckIns, missedCount: d.missedCheckIns, lastCheckIn: d.lastCheckInAt }) : null;
const mapAssetExecutors = w => {
  const perAsset = Array.isArray(w.assetExecutors) ? w.assetExecutors : [];
  return (w.assets || []).map(a => {
    const x = perAsset.find(p => p.willAssetId === a.willAssetId);
    if (x) return { willAssetId: a.willAssetId, id: x.executorAccountId, name: x.executorName, status: x.status };
    if (w.executor) return { willAssetId: a.willAssetId, id: w.executor.executorAccountId, name: w.executor.executorName, status: w.executor.status };
    return null;
  }).filter(Boolean);
};
const mapWill = w => ({ ...w, name: w.title || "", assets: (w.assets || []).map(a => ({ assetId: a.assetId, willAssetId: a.willAssetId, assetName: a.assetName, assetType: a.assetType, beneficiary: (w.assignments || []).find(x => x.willAssetId === a.willAssetId) ? (w.beneficiaries || []).find(b => b.id === (w.assignments || []).find(x => x.willAssetId === a.willAssetId)?.beneficiaryDesignationId) : null })), beneficiaries: w.beneficiaries || [], assignments: w.assignments || [], executor: w.executor ? { id: w.executor.executorAccountId, name: w.executor.executorName, status: w.executor.status, } : null, dms: mapDms(w.dms), assetExecutors: mapAssetExecutors(w), payment: null });
export const getExecutors = async () => (await r("/executors")).map(e => ({ ...e, id: e.accountId, name: e.fullName, specialization: e.bio || "", rating: e.rating || 0, experienceYears: e.experienceYears || 0, completedCases: e.completedCases || 0, feeAmount: e.serviceFee, feeCurrency: e.currency }));
export const getProfile = async () => { const u = await r("/accounts/me"); return { ...u, phone: u.phoneNumber || "", identityNumber: null, faceVerified: null }; };
export const getExecutorProfile = async (accountId) => {
  if (!accountId) {
    throw new Error("Missing Executor account ID.");
  }

  return r(`/executors/${accountId}`);
};

export const updateMyExecutorProfile = async ({
  bio,
  specialty,
  experienceYears,
}) => {
  return r("/executors/me/profile", "PUT", {
    bio,
    specialty,
    experienceYears: Number(experienceYears),
  });
};

export const updateMyExecutorPricing = async ({
  serviceFee,
  currency = "VND",
}) => {
  return r("/executors/me/pricing", "PUT", {
    serviceFee: Number(serviceFee),
    currency,
  });
};

export const sendPhoneOtp = unsupported("Phone OTP");
export const verifyPhoneOtp = unsupported("Phone OTP");
export const verifyIdentityNumber = unsupported("National ID verification");
export const verifyFace = unsupported("Face verification");
export const getAssets = async () => (await r("/assets")).map(mapAsset);

export const getAssetSecret = async (id, pinGrant) => {
  if (!pinGrant) {
    throw new Error("Security PIN is required.");
  }

  const result = await r(
    `/assets/${id}/secret`,
    "GET",
    undefined,
    {
      "X-Owner-Pin-Grant": pinGrant
    }
  );

  return result.secretData || {};
};
export const assignAssetExecutor = async (willId, willAssetId, executorAccountId) => {
  return await r(`/wills/${willId}/assets/${willAssetId}/executor`, "POST", { executorAccountId });
};
export const deleteAsset = async (id, pinGrant) => {
  if (!pinGrant) {
    throw new Error("Security PIN is required.");
  }
  return r(`/asset-maintenance/${id}`, "DELETE", undefined, {
    "X-Owner-Pin-Grant": pinGrant
  });
};
// New assets use the dedicated real-provider routes. Existing Will/Wallet APIs are unchanged.
export async function addAsset(userId, form) {
  let created;
  if (form.type === "Document") created = await uploadDocumentAsset(form);
  else if (form.type === "Online Account" || form.type === "Cloud Storage")
    created = await connectAccountAsset(form);
  else throw new Error("Unsupported new Asset type. Use an existing legacy asset if required.");
  const assets = await getAssets();
  return assets.find(asset => asset.id === created.assetId || asset.id === created.id)
    || mapAsset(created);
}


export const updateAsset = async (userId, id, form, pinGrant) => {
  if (!pinGrant) {
    throw new Error("Please verify your security PIN before editing.");
  }

  const originalType = TYPE_CODES[form.type] || form.type;

  const provider =
    originalType === "DOCUMENT"
      ? (form.accountType || "LOCAL_DOCUMENT")
      : (
          form.accountType ||
          (originalType === "CLOUD_STORAGE" ? "GOOGLE_DRIVE" : "GMAIL")
        );

  const result = await r(
    `/assets/${id}`,
    "PUT",
    {
      name: form.name,
      description: form.description || "",
      assetType: originalType,
      provider: String(provider).toUpperCase(),
      secretData: null,
      handoverPassword: form.handoverPassword || null,
      handoverConsent: !!form.handoverConsent
    },
    {
      "X-Owner-Pin-Grant": pinGrant
    }
  );

  return mapAsset(result);
};

export { checkDocumentIntegrity, downloadDocument };


export const getWills = async () => {
  const summaries = await r("/wills");

  return Promise.all(
    summaries.map(async (w) => {
      // Draft van duoc doc chi tiet de Create Will hoat dong
      if (w.status === "DRAFT") {
        try {
          return await getWill(null, w.id);
        } catch {
          // Fallback sang summary
        }
      }

      // Will da Submit: khong tu dong lay chi tiet
      return {
        ...w,
        name: w.title || "",
        assets: null,
        beneficiaries: null,
        assignments: null,
        executor: null,
        dms: null,
        payment: null
      };
    })
  );
};


export const getWill = async (userId, id, pinGrant = null) => {
  const headers = pinGrant
    ? { "X-Owner-Pin-Grant": pinGrant }
    : undefined;

  const result = await r(
    `/wills/${id}`,
    "GET",
    undefined,
    headers
  );

  return mapWill(result);
};

export async function saveWillDraft(userId, id, patch = {}) {
  if (!id) { if (!patch.name?.trim()) throw Error("Will name is required before creating a draft."); const created = await r("/wills", "POST", { title: patch.name.trim(), description: patch.description || "" }); return getWill(userId, created.id); }
  const before = await getWill(userId, id);
  if (patch.name !== undefined || patch.description !== undefined) { await r(`/wills/${id}`, "PUT", { title: patch.name ?? before.name, description: patch.description ?? before.description }); }
  if (patch.assets) { let assigned = new Set(before.assets.map(x => x.assetId)); for (const item of patch.assets) { if (!assigned.has(item.assetId)) { await r(`/wills/${id}/assets`, "POST", { assetId: item.assetId }); assigned.add(item.assetId); } } for (const a of before.assets) { if (!patch.assets.some(x => x.assetId === a.assetId)) await r(`/wills/${id}/assets/${a.willAssetId}`, "DELETE"); } }
  if (patch.executor?.id && patch.executor.id !== before.executor?.id) await r(`/wills/${id}/executor`, "POST", { executorAccountId: patch.executor.id });
  if (patch.mainExecutorId !== undefined) {
    // One main executor accepts the will, is paid and hands everything over.
    if (patch.mainExecutorId && patch.mainExecutorId !== before.executor?.id) await r(`/wills/${id}/executor`, "POST", { executorAccountId: patch.mainExecutorId });
    // Specialists are optional per-asset suggestions that need no acceptance.
    const wanted = new Map((patch.specialists || []).map(x => [x.willAssetId, x.executorId]));
    for (const a of before.assets) {
      const current = (before.assetExecutors || []).find(x => x.willAssetId === a.willAssetId)?.id;
      const want = wanted.get(a.willAssetId);
      if (want && want !== patch.mainExecutorId) { if (want !== current || current === before.executor?.id) await assignAssetExecutor(id, a.willAssetId, want); }
      else if (current) await r(`/wills/${id}/assets/${a.willAssetId}/executor`, "DELETE");
    }
  }
  if (patch.dms) await r(`/wills/${id}/dms`, "PUT", { enabled: true, checkInIntervalDays: patch.dms.checkInDays, reminderBeforeDays: patch.dms.reminderDays, maxMissedCheckIns: patch.dms.maxMissed });
  return getWill(userId, id);
}
export const deleteWillDraft = async (userId, id, declined = false) => r(declined ? `/will-maintenance/${id}/declined` : `/wills/${id}`, "DELETE");
export const submitWill = async (userId, id) => r(`/wills/${id}/submit`, "POST");
export async function updateWillBeneficiary(userId, id, assetId, person) { const w = await getWill(userId, id); const asset = w.assets.find(a => a.assetId === assetId); if (!asset) throw Error("Asset is not attached to the will"); const beneficiary = w.beneficiaries.find(b => person.accountId ? b.beneficiaryAccountId === person.accountId : b.email === person.email) || await r(`/wills/${id}/beneficiaries`, "POST", { beneficiaryAccountId: person.accountId || null, fullName: person.fullName, email: person.email, phone: person.phone, relationship: person.relationship }); await r(`/wills/${id}/assignments`, "POST", { willAssetId: asset.willAssetId, beneficiaryDesignationId: beneficiary.id }); return getWill(userId, id); }
export const updateWillOverview = (userId, id, patch) => saveWillDraft(userId, id, patch);
export const changeWillExecutor = (userId, id, executor) => saveWillDraft(userId, id, { executor });
export const updateWillDms = (userId, id, dms) => saveWillDraft(userId, id, { dms });

export const checkInDms = async (userId, id, pinGrant = null) => {
  await r(`/wills/${id}/dms/check-in`, "POST");
  return getWill(userId, id, pinGrant);
};
export const cancelDeathHandover = unsupported("Cancel death handover");

// Immediate Handover needs a fresh Security PIN grant (scope HANDOVER_START,
// resource = Will ID). The optional detailGrant is the WILL_DETAIL grant used to
// re-read the will afterwards.
export const requestImmediateHandover = async (
  userId,
  id,
  handoverGrant,
  detailGrant = null
) => {
  if (!handoverGrant) {
    throw new Error("Security PIN is required.");
  }

  const result = await r(
    `/handover/${id}/immediate`,
    "POST",
    undefined,
    { "X-Owner-Pin-Grant": handoverGrant }
  );

  // Called from Will Detail: reload the will with its existing PIN grant.
  if (detailGrant) {
    return getWill(userId, id, detailGrant);
  }

  // Called from My Wills: the caller reloads the list itself.
  return result;
};
export const getHandover = (willId) => r(`/handover/${willId}`);

export async function searchBeneficiaryAccount(q) { return r(`/accounts/search?${q.includes("@") ? "email" : "phone"}=${encodeURIComponent(q.trim())}`); }
export {
  getInheritedWills,
  getInheritedWill,
  claimInheritedItem,
  revealInheritedSecret,
} from "./inheritanceApi";
export async function getOwnerDashboardData() { const [wills, assets] = await Promise.all([getWills(), getAssets()]); const main = wills.find(w => w.status === "ACTIVE") || wills[0]; const detail = main ? await getWill(null, main.id) : null; return { vaultStatus: detail?.status || "LOCKED", deadManSwitch: detail?.dms ? { enabled: detail.dms.enabled, willId: detail.id, checkInCycleDays: detail.dms.checkInDays, lastCheckIn: detail.dms.lastCheckIn, nextCheckInDue: detail.dms.nextCheckInAt, missedCount: detail.dms.missedCount, maxMissed: detail.dms.maxMissed, caseOpen: false } : { enabled: false }, assets: assets.map(a => ({ ...a, beneficiary: null })), executors: detail?.executor ? [{ id: detail.executor.id, name: detail.executor.name, status: detail.executor.status }] : [], beneficiaries: (detail?.beneficiaries || []).map(b => ({ id: b.id, name: b.fullName, assetsAssigned: (detail.assignments || []).filter(a => a.beneficiaryDesignationId === b.id).length })), recentActivity: [] }; }

// ---- Owner: notifications, wallet, executor review ----
export const getDmsAlerts = () => r("/dms/alerts");
export const getWallet = () => r("/wallet");
export const topUpWallet = (amount) => r("/wallet/topup", "POST", { amount: Number(amount) }, { "Idempotency-Key": crypto.randomUUID() });
export const createExecutorReview = (executorAccountId, { willId, rating, comment }) => r(`/executors/${executorAccountId}/reviews`, "POST", { willId, rating: Number(rating), comment: comment || null });
