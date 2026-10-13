import { request, MOCK_MODE, ADVANCED_MOCK_MODE } from "./client";
import { getOwnerDashboardData, checkInDms, cancelDeathHandover, requestImmediateHandover } from "./mockVault";

export async function getOwnerDashboard(userId) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return getOwnerDashboardData(userId);
  return request("/owner/dashboard", { method: "GET" });
}

export async function confirmStillActive({ userId, willId }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return checkInDms(userId, willId);
  return request(`/owner/wills/${willId}/check-in`, { method: "POST" });
}

export async function cancelHandover({ userId, willId }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return cancelDeathHandover(userId, willId);
  return request(`/owner/wills/${willId}/cancel-handover`, { method: "POST" });
}

export async function requestHandover({ userId, willId }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return requestImmediateHandover(userId, willId);
  return request(`/owner/wills/${willId}/request-handover`, { method: "POST" });
}
