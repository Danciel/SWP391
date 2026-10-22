export const ACCOUNT_PROVIDERS = ["Binance", "Gmail", "Microsoft", "GitHub", "Dropbox"];

export const ENABLED_ASSET_TYPES = ["Online Account", "Document", "Other"];

export const NOT_VERIFIABLE_TYPES = ["Document", "Other"];

export const isVerifiable = (type) => !NOT_VERIFIABLE_TYPES.includes(type);

export function providerLabel(provider) {
  if (!provider) return "";
  return ACCOUNT_PROVIDERS.find((p) => p.toUpperCase() === String(provider).toUpperCase()) || provider;
}

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
