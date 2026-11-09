import { request, getToken } from "./client";
import { unsupported } from "./unsupported";

// Danh sách Legal Case dành cho Legal Verifier
export const getLegalVerifierDashboard = async () => {
  const result = await request("/legal-cases");

  const cases = Array.isArray(result)
    ? result
    : result?.items ?? result?.cases ?? [];

  return {
    cases,
    stats: {},
    recentActivity: [],
  };
};

// Xem chi tiết một Legal Case
export const getLegalCaseDetails = (caseId) => {
  if (!caseId) {
    throw new Error("Missing Legal Case ID.");
  }

  return request(`/legal-cases/${caseId}`);
};

export const getLegalCaseCertificate = async (caseId) => {
  if (!caseId) {
    throw new Error("Missing Legal Case ID.");
  }

  const token = getToken();

  if (!token) {
    throw new Error("Không tìm thấy token. Vui lòng đăng nhập lại.");
  }

  const response = await fetch(
    `/api/legal-cases/${caseId}/certificate`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw new Error(
      `Không thể tải giấy chứng tử (HTTP ${response.status}).`
    );
  }

  return response;
};




export const reviewLegalCase = ({ caseId, approve, reason }) => {
  if (!caseId) {
    throw new Error("Missing Legal Case ID.");
  }

  return request(`/legal-cases/${caseId}/decision`, {
    method: "POST",
    body: {
      approve,
      note: approve ? null : reason?.trim(),
    },
  });
};


// Giữ lại hàm cũ, chưa xác nhận API báo cáo gian lận
export const flagSuspectedFraud = unsupported("Fraud reporting");
