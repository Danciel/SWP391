import { request, MOCK_MODE } from "./client";
import * as mock from "./mockBackend";

export async function getAdminDashboard() {
  if (MOCK_MODE) return mock.getAdminDashboard();
  return request("/admin/dashboard", { method: "GET" });
}

export async function setAccountStatus({ accountId, status }) {
  if (MOCK_MODE) return mock.setAccountStatus({ accountId, status });
  return request(`/admin/accounts/${accountId}/status`, {
    method: "POST",
    body: { status },
  });
}

export async function setAccountRole({ accountId, role }) {
  if (MOCK_MODE) return mock.setAccountRole({ accountId, role });
  return request(`/admin/accounts/${accountId}/role`, {
    method: "POST",
    body: { role },
  });
}
