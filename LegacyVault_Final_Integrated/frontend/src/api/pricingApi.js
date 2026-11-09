import { request } from "./client";

// Executor: system suggestion + how it was built
export const getMyPricingSuggestion = () => request("/executors/me/pricing-suggestion");

// Executor: upload evaluation CSV (experience, rating, cases, wins)
export const uploadEvaluationCsv = (file) => {
  const formData = new FormData();
  formData.append("file", file);
  return request("/executors/me/personal-csv", { method: "POST", body: formData });
};

// The sample file is built in the browser, so no authenticated download is needed.
export function downloadEvaluationTemplate() {
  const text = "experience_years,rating,completed_cases,won_cases\r\n6,4.5,40,32\r\n";
  const url = URL.createObjectURL(new Blob([text], { type: "text/csv" }));
  const a = document.createElement("a");
  a.href = url;
  a.download = "executor-evaluation-template.csv";
  a.click();
  URL.revokeObjectURL(url);
}

// Admin: weights behind the suggested fee
export const getPricingRules = () => request("/admin/executor-pricing-rules");
export const updatePricingRules = (rules) =>
  request("/admin/executor-pricing-rules", { method: "PUT", body: rules });
