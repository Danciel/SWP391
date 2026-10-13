import { request, MOCK_MODE, ADVANCED_MOCK_MODE } from "./client";
import * as mock from "./mockBackend";

export async function getExecutorDashboard() {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.getExecutorDashboard();
  return request("/executor/dashboard", { method: "GET" });
}

export async function respondToExecutorRequest({ requestId, accept }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.respondToExecutorRequest({ requestId, accept });
  return request(`/executor/requests/${requestId}/respond`, {
    method: "POST",
    body: { accept },
  });
}

export async function openExecutorCase({ triggerId }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.openExecutorCase({ triggerId });
  return request(`/executor/dms-triggers/${triggerId}/open-case`, { method: "POST" });
}

export async function submitExecutorLegalDocuments({ caseId, documentNames }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.submitExecutorLegalDocuments({ caseId, documentNames });
  return request(`/executor/cases/${caseId}/documents`, {
    method: "POST",
    body: { documentNames },
  });
}

export async function verifyCaseProfile({ caseId }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.verifyCaseProfile({ caseId });
  return request(`/executor/cases/${caseId}/verify-profile`, { method: "POST" });
}

export async function getExecutorFee() {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.getExecutorFee();
  return request("/executor/fee", { method: "GET" });
}

export async function setExecutorFee({ fee }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.setExecutorFee({ fee });
  return request("/executor/fee", { method: "PUT", body: { fee } });
}

export async function respondToHandoverRequest({ requestId, confirm }) {
  if (MOCK_MODE || ADVANCED_MOCK_MODE) return mock.respondToHandoverRequest({ requestId, confirm });
  return request(`/executor/handover-requests/${requestId}/respond`, {
    method: "POST",
    body: { confirm },
  });
}
