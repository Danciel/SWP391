import { request, MOCK_MODE } from "./client";
import * as mock from "./mockBackend";

export async function getLegalVerifierDashboard() {
  if (MOCK_MODE) return mock.getLegalVerifierDashboard();
  return request("/verifier/dashboard", { method: "GET" });
}

export async function reviewLegalCase({ caseId, approve, reason, willVerified }) {
  if (MOCK_MODE) return mock.reviewLegalCase({ caseId, approve, reason, willVerified });
  return request(`/verifier/cases/${caseId}/review`, {
    method: "POST",
    body: { approve, reason, willVerified },
  });
}

export async function flagSuspectedFraud({ caseId, reason }) {
  if (MOCK_MODE) return mock.flagSuspectedFraud({ caseId, reason });
  return request(`/verifier/cases/${caseId}/flag`, {
    method: "POST",
    body: { reason },
  });
}
