import { request } from "./client";

const PIN_HEADER = "X-Owner-Pin-Grant";

// Beneficiary: handovers that contain assets for me (no secrets, no PIN).
export const getInheritedWills = () => request("/inheritance");

// Beneficiary: open one will. Needs a Security PIN grant
// (scope INHERITANCE_VIEW, resource = Will ID).
export const getInheritedWill = (willId, pinGrant) => {
  if (!pinGrant) throw new Error("Security PIN is required.");
  return request(`/inheritance/${willId}`, {
    headers: { [PIN_HEADER]: pinGrant },
  });
};

// Beneficiary: receive one asset by scanning the face (JPEG blob).
export const claimInheritedItem = (itemId, faceBlob) => {
  if (!faceBlob) throw new Error("Face image is required.");
  const form = new FormData();
  form.append("faceImage", faceBlob, "face.jpg");
  return request(`/inheritance/items/${itemId}/claim`, {
    method: "POST",
    body: form,
  });
};

// Beneficiary: type the access code that was emailed (XXXX-XXXX-XXXX).
// The Security PIN comes first (grant scope INHERITANCE_VIEW, resource = Will ID).
// 3 wrong codes lock it for 15 minutes (the server answers 429).
export const verifyAccessCode = (willId, code, pinGrant) => {
  if (!pinGrant) throw new Error("Security PIN is required.");
  return request(`/inheritance/${willId}/access-code/verify`, {
    method: "POST",
    body: { code },
    headers: { [PIN_HEADER]: pinGrant },
  });
};

// Beneficiary: ask for a NEW access code by email (the old one stops working).
export const resendAccessCode = (willId, pinGrant) => {
  if (!pinGrant) throw new Error("Security PIN is required.");
  return request(`/inheritance/${willId}/access-code/resend`, {
    method: "POST",
    headers: { [PIN_HEADER]: pinGrant },
  });
};

// Beneficiary: all new codes were used (or the code is locked) -> ask for help.
// The Executor is emailed and the request is kept for the support team.
export const requestAccessCodeHelp = (willId, pinGrant) => {
  if (!pinGrant) throw new Error("Security PIN is required.");
  return request(`/inheritance/${willId}/access-code/support`, {
    method: "POST",
    headers: { [PIN_HEADER]: pinGrant },
  });
};

// Beneficiary: credentials of an asset that was already claimed.
export const revealInheritedSecret = (willId, itemId, pinGrant) => {
  if (!pinGrant) throw new Error("Security PIN is required.");
  return request(`/inheritance/${willId}/items/${itemId}/secret`, {
    headers: { [PIN_HEADER]: pinGrant },
  });
};
