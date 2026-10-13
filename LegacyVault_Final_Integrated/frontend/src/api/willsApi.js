import { request } from "./client";

export function createWill({ title, description }) {
  return request("/wills", {
    method: "POST",
    body: { title, description: description || null },
  });
}

export function updateWill(id, { title, description }) {
  return request(`/wills/${id}`, {
    method: "PUT",
    body: { title, description: description || null },
  });
}

export function getWills() {
  return request("/wills");
}

export function getWill(id) {
  return request(`/wills/${id}`);
}

export function deleteWill(id) {
  return request(`/wills/${id}`, { method: "DELETE" });
}

export function addWillAsset(willId, assetId) {
  return request(`/wills/${willId}/assets`, {
    method: "POST",
    body: { assetId },
  });
}

export function addBeneficiary(willId, beneficiary) {
  return request(`/wills/${willId}/beneficiaries`, {
    method: "POST",
    body: beneficiary,
  });
}

export function assignAsset(willId, willAssetId, beneficiaryDesignationId) {
  return request(`/wills/${willId}/assignments`, {
    method: "POST",
    body: { willAssetId, beneficiaryDesignationId },
  });
}

export function getExecutors() {
  return request("/executors");
}

export function assignExecutor(willId, executorAccountId) {
  return request(`/wills/${willId}/executor`, {
    method: "POST",
    body: { executorAccountId },
  });
}

export function configureDms(willId, config) {
  return request(`/wills/${willId}/dms`, {
    method: "PUT",
    body: config,
  });
}

export function checkInDms(willId) {
  return request(`/wills/${willId}/dms/check-in`, { method: "POST" });
}

export function submitWill(willId) {
  return request(`/wills/${willId}/submit`, { method: "POST" });
}
