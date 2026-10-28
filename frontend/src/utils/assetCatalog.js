<<<<<<< Updated upstream:frontend/src/utils/assetCatalog.js
// LV-04 / LV-05 / LV-07 — what the UI offers must match what the backend can really handle.
//
// Source of truth = backend/LegacyVault/database/seed_mock_external_accounts_if_needed.sql
// (MockExternalAccounts) + ValidatedAssetsController:
//   * ACCOUNT assets are only created when a MockExternalAccounts row exists for the Provider
//   * DIGITAL_WALLET / BANK_ACCOUNT / CLOUD_STORAGE use Provider == AssetType, and the seed has
//     NO rows for those providers, so creating them is rejected today
//   * DOCUMENT / OTHER need no external login and therefore can NOT be verified
//
// If the team confirms more providers in the DB, add them here — nothing else needs to change.
export const ACCOUNT_PROVIDERS = ["Binance", "Gmail", "Microsoft", "GitHub", "Dropbox"];

// Types the user may pick when creating an asset.
export const ENABLED_ASSET_TYPES = ["Online Account", "Document", "Other"];

// Types that have no external login, so no verification mechanism exists.
export const NOT_VERIFIABLE_TYPES = ["Document", "Other"];

export const isVerifiable = (type) => !NOT_VERIFIABLE_TYPES.includes(type);
=======
export const ACCOUNT_PROVIDERS = ["GMAIL", "GITHUB"];
export const ENABLED_ASSET_TYPES = ["Document", "Online Account", "Cloud Storage"];
export const NOT_VERIFIABLE_TYPES = ["Other", "Crypto Wallet", "Bank Account"];
export const isVerifiable = type => !NOT_VERIFIABLE_TYPES.includes(type);
>>>>>>> Stashed changes:LegacyVault_Final_Integrated/frontend/src/utils/assetCatalog.js

// Backend stores provider upper-cased ("GMAIL"); map it back to the label shown in the UI.
export function providerLabel(provider) {
  const names = { GMAIL: "Google / Gmail", GITHUB: "GitHub", GOOGLE_DRIVE: "Google Drive", LOCAL_DOCUMENT: "Local Document" };
  const code = String(provider || "").toUpperCase();
  return names[code] || provider || "";
}

// Backend VerificationStatus: UNVERIFIED | VERIFIED | FAILED
export function verificationInfo(asset) {
  const status = String(asset.verificationStatus || "").toUpperCase();
  if (["PROVIDER_VERIFIED", "FILE_ACCESS_VERIFIED", "INTEGRITY_VERIFIED", "VERIFIED"].includes(status)) {
    return { key: "VERIFIED", label: "Verified", hint: asset.provider === "GMAIL" || asset.provider === "GITHUB" ? "Account identity verified by OAuth. Handover password is not checked." : "Asset verification completed." };
  }
  if (status === "FAILED" || status === "INTEGRITY_FAILED")
    return { key: "FAILED", label: "Verification failed", hint: "Verification did not succeed." };
  return { key: "UNVERIFIED", label: "Not verified yet", hint: "Connect with the provider or verify the document." };
}
