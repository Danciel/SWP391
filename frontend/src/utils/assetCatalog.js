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

// Backend stores provider upper-cased ("GMAIL"); map it back to the label shown in the UI.
export function providerLabel(provider) {
  if (!provider) return "";
  return ACCOUNT_PROVIDERS.find((p) => p.toUpperCase() === String(provider).toUpperCase()) || provider;
}

// Backend VerificationStatus: UNVERIFIED | VERIFIED | FAILED
export function verificationInfo(asset) {
  if (!isVerifiable(asset.type)) {
    return { key: "NA", label: "Not verifiable", hint: "Stored content only — no provider check exists for this type." };
  }
  switch (asset.verificationStatus) {
    case "VERIFIED":
      return { key: "VERIFIED", label: "Verified (mock provider)", hint: "Credentials matched the demo provider account." };
    case "FAILED":
      return { key: "FAILED", label: "Verification failed", hint: "Credentials did not match the demo provider account." };
    default:
      return { key: "UNVERIFIED", label: "Not verified yet", hint: "Press Verify to check the credentials." };
  }
}
