
import { request } from "./client";
import { unsupported } from "./unsupported";

// Executor inbox and DMS

export const getExecutorDashboard = async () => {
    const [inboxResult, dmsResult] = await Promise.all([
        request("/executor-inbox"),
        request("/executor-inbox/triggers"),
    ]);

    const requests = Array.isArray(inboxResult) ? inboxResult : [];
    const dmsTriggers = Array.isArray(dmsResult) ? dmsResult : [];

    let cases = [];

    try {
        const result = await getExecutorLegalCases();
        cases = Array.isArray(result) ? result : [];
    } catch (error) {
        console.error("Failed to load Legal Cases:", error);
    }

    const activeWills = requests.filter(
        (item) => item.status === "ACCEPTED"
    );

    
    // LV-32: Load handover details for assigned Wills.
    const handoverResults = await Promise.all(
        activeWills.map(async (will) => {
            const willId = will.willId ?? will.id;

            if (!willId) return null;

            try {
                const result = await getHandoverByWill(willId);

                if (!result) return null;

                const status = result.status ?? result.handoverStatus;
                const willStatus = result.willStatus;

                // Include a handover only when its status confirms it is active.
                if (
                    status !== "IN_PROGRESS" &&
                    status !== "HANDOVER_IN_PROGRESS" &&
                    willStatus !== "HANDOVER_IN_PROGRESS"
                ) {
                    return null;
                }

                return {
                    id: result.id ?? willId,
                    willId,
                    ownerName:
                        result.ownerName ?? will.ownerName ?? "Unknown Owner",
                    willTitle:
                        result.willTitle ??
                        result.title ??
                        will.willTitle ??
                        "Untitled Will",
                    status,
                    requestedAt:
                        result.startedAt ??
                        result.requestedAt ??
                        result.createdAt ??
                        null,
                    deadlineAt: result.deadlineAt ?? null,
                    totalItems: result.totalItems ?? result.items?.length ?? 0,
                    deliveredItems: result.deliveredItems ?? 0,
                    assets: Array.isArray(result.items)
                        ? result.items.map((item) => ({
                            id: item.id ?? item.itemId,
                            name: item.assetName ?? item.name ?? "Asset",
                            type: item.assetType ?? item.type ?? "—",
                            beneficiaryName:
                                item.beneficiaryName ?? "Not assigned",
                            relationship: item.relationship ?? "",
                            allocationPercentage:
                                item.allocationPercentage ?? null,
                            status: item.status ?? "PENDING",
                        }))
                        : [],
                };
            } catch (error) {
                // A Will without a handover record is not a dashboard failure.
                console.warn(
                    `No accessible handover for Will ${willId}:`,
                    error.message
                );
                return null;
            }
        })
    );

    console.log(
        "LV-32 handoverRequests:",
        handoverResults.filter(Boolean)
    );


    return {
        hireRequests: [],
        requests,
        activeWills,
        cases,
        dmsTriggers,
        handoverRequests: handoverResults.filter(Boolean),
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

// Executor removes a DECLINED request from the dashboard (needs a PIN grant).
export const deleteExecutorRequest = ({ requestId, pinGrant }) => {
    if (!pinGrant) {
        throw new Error("Security PIN is required.");
    }
    return request(`/executor-inbox/${requestId}`, {
        method: "DELETE",
        headers: { "X-Owner-Pin-Grant": pinGrant },
    });
};

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

export const getHandoverByWill = (willId) => {
    if (!willId) {
        throw new Error("Missing Will ID.");
    }

    return request(`/handover/${willId}`);
};

export const respondToHandoverRequest = unsupported(
    "Executor handover response"
);

export const getExecutorDmsTriggers = () =>
    request("/executor-inbox/triggers");
