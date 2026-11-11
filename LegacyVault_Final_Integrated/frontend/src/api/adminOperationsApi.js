import { request } from "./client";

/**
 * GET /api/admin/payments
 * Admin xem danh sách thanh toán và Ledger.
 */
export async function getAdminPayments(params = {}) {
  const query = new URLSearchParams();

  query.set("page", String(params.page ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));

  if (params.status) {
    query.set("status", params.status);
  }

  if (params.willId) {
    query.set("willId", params.willId);
  }

  const response = await request(
    `/admin/payments?${query.toString()}`
  );

  return response?.data ?? response;
}


export async function getAdminAuditLogs(params = {}) {
  const query = new URLSearchParams();

  query.set("page", String(params.page ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));

  if (params.action) {
    query.set("action", params.action);
  }

  if (params.entityType) {
    query.set("entityType", params.entityType);
  }

  if (params.actorAccountId) {
    query.set("actorAccountId", params.actorAccountId);
  }

  if (params.from) {
    query.set("from", params.from);
  }

  if (params.to) {
    query.set("to", params.to);
  }

  const response = await request(
    `/admin/auditlogs?${query.toString()}`
  );

  return response?.data ?? response;
}
