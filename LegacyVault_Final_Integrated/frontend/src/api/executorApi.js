import { request } from "./client";
import { unsupported } from "./unsupported";



export const getExecutorDashboard = async () => {
    const [requests, dmsTriggers] = await Promise.all([
        request("/executor-inbox"),
        request("/executor-inbox/triggers"),
    ]);

    return {
        hireRequests: [],
        requests,
        activeWills: requests.filter(
            (r) => r.status === "ACCEPTED"
        ),
        cases: [],
        dmsTriggers,
        handoverRequests: [],
        stats: {},
    };
};

export const getExecutorReviews = (executorId) => request(`/executors/${executorId}/reviews`);
export const getExecutorFee = async () => ({ fee: null });
export const setExecutorFee = unsupported("Executor fee management");
export const respondToExecutorRequest = ({ requestId, accept }) => request(`/executor-inbox/${requestId}/respond`, { method: "POST", body: { accept } });
export const openExecutorCase = unsupported("Executor case management");
export const submitExecutorLegalDocuments = unsupported("Executor document submission");
export const verifyCaseProfile = unsupported("Executor verification");
export const respondToHandoverRequest = unsupported("Executor handover response");
export const getExecutorDmsTriggers = async () => {
    return request("/executor-inbox/triggers");
};

