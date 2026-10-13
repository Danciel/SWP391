import { request } from "./client";

const UI_TO_API_TYPE = {
  Document: "DOCUMENT",
  "Crypto Wallet": "DIGITAL_WALLET",
  "Online Account": "ACCOUNT",
  "Cloud Storage": "CLOUD_STORAGE",
  "Bank Account": "BANK_ACCOUNT",
  Other: "OTHER",
};

const API_TO_UI_TYPE = Object.fromEntries(
  Object.entries(UI_TO_API_TYPE).map(([ui, api]) => [api, ui])
);

export function toUiAsset(asset) {
  return {
    ...asset,
    type: API_TO_UI_TYPE[asset.assetType] || asset.assetType,
    accountType: asset.provider || "",
  };
}

function buildSecretData(form) {
  if (form.type === "Online Account") {
    return {
      username: form.username?.trim() || "",
      password: form.password || "",
    };
  }

  const secret = {};
  if (form.username?.trim()) secret.username = form.username.trim();
  if (form.password) secret.password = form.password;
  if (Object.keys(secret).length === 0) {
    secret.note = form.description?.trim() || form.name?.trim() || "LegacyVault asset";
  }
  return secret;
}

export async function getAssets() {
  const data = await request("/assets");
  return (data || []).map(toUiAsset);
}

export async function getAsset(id) {
  return toUiAsset(await request(`/assets/${id}`));
}

export async function addAsset(form) {
  if (form.type === "Online Account" && (!form.username?.trim() || !form.password)) {
    throw new Error("Online Account requires username and password.");
  }

  const data = await request("/assets", {
    method: "POST",
    body: {
      name: form.name.trim(),
      description: form.description?.trim() || null,
      assetType: UI_TO_API_TYPE[form.type] || "OTHER",
      provider: form.type === "Online Account" ? form.accountType : null,
      secretData: buildSecretData(form),
    },
  });
  return toUiAsset(data);
}

export async function updateAsset(id, form, { replaceSecret = false } = {}) {
  const body = {
    name: form.name.trim(),
    description: form.description?.trim() || null,
    assetType: UI_TO_API_TYPE[form.type] || "OTHER",
    provider: form.type === "Online Account" ? form.accountType : null,
    secretData: replaceSecret ? buildSecretData(form) : null,
  };
  return toUiAsset(await request(`/assets/${id}`, { method: "PUT", body }));
}

export function deleteAsset(id) {
  return request(`/assets/${id}`, { method: "DELETE" });
}

export function revealAssetSecret(id) {
  return request(`/assets/${id}/secret`);
}

export function verifyAsset(id) {
  return request(`/assets/${id}/verify`, { method: "POST" });
}
