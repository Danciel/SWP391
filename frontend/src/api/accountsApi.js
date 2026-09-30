import { request } from "./client";

export function getAccounts() {
  return request("/accounts", { method: "GET", auth: false });
}

export function createAccount(account) {
  return request("/accounts", {
    method: "POST",
    auth: false,
    body: account,
  });
}
