// Real-API replacement for the WILL-related functions in src/api/mockVault.js (same names/signatures).
// Usage: copy to src/api/willApi.js, change the imports in the pages from "../../api/mockVault" to
// "../../api/willApi" for these functions, and set MOCK_MODE = false in client.js.
// `userId` params are kept for compatibility but ignored: the backend reads the user from the token.
import { request } from "./client";

const send = (method, body) => ({ method, body });

export const getWills = () => request("/wills");
export const getWill = (_u, id) => request(`/wills/${id}`);
export const deleteWillDraft = (_u, id) => request(`/wills/${id}`, send("DELETE"));
export const updateWillOverview = (_u, id, { name, description }) =>
  request(`/wills/${id}`, send("PUT", { name, description }));

export const getExecutors = () => request("/executors"); // replaces the hardcoded EXECUTORS constant
export const getWillReview = (id) => request(`/wills/${id}/review`);
export const submitWill = (_u, id) => request(`/wills/${id}/submit`, send("POST"));

export const updateWillDms = (_u, id, { checkInDays, reminderDays, maxMissed }) =>
  request(`/wills/${id}/dms`, send("PUT", { checkInDays, reminderDays, maxMissed }));
export const checkInDms = (_u, id) => request(`/wills/${id}/dms/check-in`, send("POST"));

export const changeWillExecutor = (_u, id, executor) =>
  request(`/wills/${id}/executor`, send("PUT", { executorId: executor.id }));

/**
 * Same "patch" style the wizard uses: { name, description, assets, executor, dms }.
 * willId === null creates the draft first.
 */
export async function saveWillDraft(_u, willId, patch = {}) {
  let will;
  if (!willId) {
    will = await request("/wills", send("POST", { name: patch.name, description: patch.description }));
  } else if (patch.name !== undefined || patch.description !== undefined) {
    will = await request(`/wills/${willId}`, send("PUT", { name: patch.name, description: patch.description }));
  }
  const id = willId || will.id;

  if (patch.assets) {
    will = await request(`/wills/${id}/assets`, send("PUT", { assetIds: patch.assets.map((a) => a.assetId) }));
  }
  if (patch.executor) will = await changeWillExecutor(null, id, patch.executor);
  if (patch.dms) will = await updateWillDms(null, id, patch.dms);
  return will || getWill(null, id);
}

/** beneficiary === null removes the beneficiary from the asset. */
export async function updateWillBeneficiary(_u, willId, assetId, beneficiary) {
  if (!beneficiary) {
    return request(`/wills/${willId}/assignments`, send("POST", { assetId, beneficiaryId: null }));
  }
  const withB = await request(`/wills/${willId}/beneficiaries`, send("POST", {
    accountId: beneficiary.accountId || null,
    fullName: beneficiary.fullName,
    email: beneficiary.email,
    phone: beneficiary.phone,
    identityNumber: beneficiary.identityNumber || null,
    relationship: beneficiary.relationship,
  }));
  // the backend de-duplicates by accountId/email, so pick the one we just sent
  const email = (beneficiary.email || "").toLowerCase();
  const b = withB.beneficiaries.find(
    (x) => (beneficiary.accountId && x.accountId === beneficiary.accountId) || x.email.toLowerCase() === email
  );
  return request(`/wills/${willId}/assignments`, send("POST", { assetId, beneficiaryId: b.id }));
}
