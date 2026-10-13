import { request } from "./client";

export function getMe() {
  return request("/accounts/me", { method: "GET" });
}

// Partial update: omitted/null fields are left unchanged.
export function updateMe({ fullName, email, phone }) {
  return request("/accounts/me", {
    method: "PUT",
    body: { fullName, email, phone },
  });
}

// Exact-match lookup; provide exactly one of email / phone.
// Admins get the full account, others get { id, username, role, identityStatus }.
// Resolves to null when not found.
export async function searchAccount({ email, phone }) {
  if (!!email === !!phone) throw new Error("Provide exactly one of 'email' or 'phone'.");
  const qs = email
    ? `email=${encodeURIComponent(email)}`
    : `phone=${encodeURIComponent(phone)}`;
  try {
    return await request(`/accounts/search?${qs}`, { method: "GET" });
  } catch (err) {
    if (err?.status === 404) return null;
    throw err;
  }
}
