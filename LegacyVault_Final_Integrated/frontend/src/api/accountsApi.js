import { request } from "./client";

export function getMe() {
  return request("/accounts/me");
}

export async function searchAccount(value) {
  const trimmed = String(value || "").trim();
  if (!trimmed) throw new Error("Enter an email or phone number.");

  const query = trimmed.includes("@")
    ? `email=${encodeURIComponent(trimmed)}`
    : `phone=${encodeURIComponent(trimmed)}`;

  return request(`/accounts/search?${query}`);
}
