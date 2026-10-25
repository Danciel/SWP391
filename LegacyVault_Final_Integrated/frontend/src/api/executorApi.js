
import { request } from "./client";
import { unsupported } from "./unsupported";

// Executor inbox and DMS
export const getExecutorDashboard = async () => {
  const [requests, dmsTriggers] = await Promise.all([
    request("/executor-inbox"),
    request("/executor-inbox/triggers"),
  ]);

  let cases = [];

  try {
    const result = await getExecutorLegalCases();
    cases = Array.isArray(result) ? result : [];
  } catch (error) {
    // A Legal Cases API error must not hide existing inbox/DMS data.
    console.error("Failed to load Legal Cases:", error);
  }

  return {
    hireRequests: [],
    requests,
    activeWills: requests.filter((item) => item.status === "ACCEPTED"),
    cases,
    dmsTriggers,
    handoverRequests: [],
    stats: {},
  };
};

export const getExecutorReviews = (executorId) =>
  request(`/executors/${executorId}/reviews`);

export const getExecutorFee = async () => ({ fee: null });

export const setExecutorFee = unsupported("Executor fee management");

export const respondToExecutorRequest = ({ requestId, accept }) =>
  request(`/executor-inbox/${requestId}/respond`, {
    method: "POST",
    body: { accept },
  });

// LV-29: list cases belonging to the authenticated Executor.
export const getExecutorLegalCases = (status) => {
  const query = status
    ? `?status=${encodeURIComponent(status)}`
    : "";

  return request(`/legal-cases${query}`);
};

// LV-29: get case details.
export const getExecutorLegalCase = (caseId) =>
  request(`/legal-cases/${caseId}`);

// LV-29: upload one file.
// Backend requires multipart/form-data: willId + file.
export const submitExecutorLegalDocuments = async ({ willId, file }) => {
  if (!willId) {
    throw new Error("Missing willId.");
  }

  if (!(file instanceof File)) {
    throw new Error("Please select a file to upload.");
  }

  const allowedExtensions = /\.(pdf|jpe?g|png)$/i;

  if (!allowedExtensions.test(file.name)) {
    throw new Error("Only PDF, JPG, and PNG files are accepted.");
  }

  if (file.size > 5 * 1024 * 1024) {
    throw new Error("The file must be 5 MB or smaller.");
  }

  const formData = new FormData();
  formData.append("willId", willId);
  formData.append("file", file);

  return request("/legal-cases", {
    method: "POST",
    body: formData,
  });
};

// Retrieve case certificate.
// Use a Blob-aware download helper for browser downloads.
export const getExecutorLegalCaseCertificate = (caseId) =>
  request(`/legal-cases/${caseId}/certificate`);

// These actions have no corresponding endpoint in the supplied controller.
export const openExecutorCase = unsupported(
  "Opening a Legal Case from a DMS trigger"
);

export const verifyCaseProfile = unsupported(
  "Owner profile verification"
);

export const respondToHandoverRequest = unsupported(
  "Executor handover response"
);

export const getExecutorDmsTriggers = () =>
  request("/executor-inbox/triggers");
