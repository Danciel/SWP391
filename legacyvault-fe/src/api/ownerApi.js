import { request, MOCK_MODE } from "./client";
import { getOwnerDashboardData, checkInDms, cancelDeathHandover, requestImmediateHandover } from "./mockVault";

export async function getOwnerDashboard(userId) {
  if (MOCK_MODE) return getOwnerDashboardData(userId);
  return request("/owner/dashboard", { method: "GET" });
}

// "I'm still active" — a normal Dead Man's Switch check-in.
export async function confirmStillActive({ userId, willId }) {
  if (MOCK_MODE) return checkInDms(userId, willId);
  return request(`/owner/wills/${willId}/check-in`, { method: "POST" });
}

// "Cancel handover" — calls off an in-progress executor/verifier case while
// it's still open.
export async function cancelHandover({ userId, willId }) {
  if (MOCK_MODE) return cancelDeathHandover(userId, willId);
  return request(`/owner/wills/${willId}/cancel-handover`, { method: "POST" });
}

// "Immediate handover" — Owner voluntarily hands the will's assets to their
// Executor now, while still alive; the Executor must confirm/decline.
export async function requestHandover({ userId, willId }) {
  if (MOCK_MODE) return requestImmediateHandover(userId, willId);
  return request(`/owner/wills/${willId}/request-handover`, { method: "POST" });
}
