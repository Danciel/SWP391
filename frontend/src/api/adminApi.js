import { request, MOCK_MODE } from "./client";
import * as mock from "./mockBackend";

const notSupported = (name) =>
  Promise.reject(new Error(`${name} is not supported by the backend yet.`));

// No backend endpoint yet.
export async function getAdminDashboard() {
  if (MOCK_MODE) return mock.getAdminDashboard();
  return notSupported("getAdminDashboard");
}

// No backend endpoint yet.
export async function setAccountStatus({ accountId, status }) {
  if (MOCK_MODE) return mock.setAccountStatus({ accountId, status });
  return notSupported("setAccountStatus");
}

// ADMIN only. Cannot change your own role. Existing JWTs keep the old role until they expire.
export async function setAccountRole({ accountId, role }) {
  if (MOCK_MODE) return mock.setAccountRole({ accountId, role });
  return request(`/accounts/${accountId}/role`, {
    method: "PUT",
    body: { role },
  });
}
