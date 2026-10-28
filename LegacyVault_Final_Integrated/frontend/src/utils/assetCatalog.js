export const ACCOUNT_PROVIDERS = ["GMAIL", "GITHUB"];
export const ENABLED_ASSET_TYPES = ["Document", "Online Account", "Cloud Storage"];
export const NOT_VERIFIABLE_TYPES = ["Other", "Crypto Wallet", "Bank Account"];
export const isVerifiable = type => !NOT_VERIFIABLE_TYPES.includes(type);

export function providerLabel(provider) {
  const names = { GMAIL: "Google / Gmail", GITHUB: "GitHub", GOOGLE_DRIVE: "Google Drive", LOCAL_DOCUMENT: "Local Document" };
  const code = String(provider || "").toUpperCase();
  return names[code] || provider || "";
}

export function verificationInfo(asset) {
  const status = String(asset.verificationStatus || "").toUpperCase();
  if (["PROVIDER_VERIFIED", "FILE_ACCESS_VERIFIED", "INTEGRITY_VERIFIED", "VERIFIED"].includes(status)) {
    return { key: "VERIFIED", label: "Verified", hint: asset.provider === "GMAIL" || asset.provider === "GITHUB" ? "Account identity verified by OAuth. Handover password is not checked." : "Asset verification completed." };
  }
  if (status === "FAILED" || status === "INTEGRITY_FAILED")
    return { key: "FAILED", label: "Verification failed", hint: "Verification did not succeed." };
  return { key: "UNVERIFIED", label: "Not verified yet", hint: "Connect with the provider or verify the document." };
}
