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

// Beneficiary: credentials of an asset that was already claimed.
export const revealInheritedSecret = (willId, itemId, pinGrant) => {
  if (!pinGrant) throw new Error("Security PIN is required.");
  return request(`/inheritance/${willId}/items/${itemId}/secret`, {
    headers: { [PIN_HEADER]: pinGrant },
  });
};
