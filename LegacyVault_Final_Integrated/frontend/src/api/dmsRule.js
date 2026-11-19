import { request } from "./client";

/**
 * DMS Rule API — singleton rule managed by admins, read (partially) by owners.
 *
 * Backend:
 *   GET    /api/admin/dms-rule   [Authorize(Roles = "ADMIN")]
 *   POST   /api/admin/dms-rule   [Authorize(Roles = "ADMIN")]
 *   PUT    /api/admin/dms-rule   [Authorize(Roles = "ADMIN")]
 *   DELETE /api/admin/dms-rule   [Authorize(Roles = "ADMIN")]
 *   GET    /api/wills/dms-rule   [Authorize]              ← owner read
 *   PUT    /api/wills/{id}/dms   [Authorize + PIN grant]  ← owner writes DMS
 *
 * Singleton semantics (server-enforced):
 *   GET    /admin/dms-rule  → 404 when the rule has NOT been created yet.
 *   POST   /admin/dms-rule  → 409 when a rule already exists (use PUT).
 *   PUT    /admin/dms-rule  → 404 when no rule exists yet       (use POST).
 *   DELETE /admin/dms-rule  → 204 on success, 404 if none exists.
 *   GET    /wills/dms-rule  → 404 until the admin creates a rule.
 *
 * Cross-field invariants enforced server-side (DmsRuleService.Validate):
 *   • MinCheckInIntervalDays <= MaxCheckInIntervalDays
 *   • ReminderBeforeDays    <  MinCheckInIntervalDays   (STRICT)
 *     — the strict "<" is what guarantees every owner choice is valid under
 *       the existing backend rule `ReminderBeforeDays < CheckInIntervalDays`.
 *   • All five values are ints inside their C# [Range] bounds:
 *       ReminderBeforeDays     ∈ [0, 3649]
 *       MaxMissedCheckIns      ∈ [1, 100]
 *       GraceDays              ∈ [0, 30]
 *       MinCheckInIntervalDays ∈ [1, 3650]
 *       MaxCheckInIntervalDays ∈ [1, 3650]
 */

// ──────────────────────────────────────────────────────────────────────────
// Client-side validation — MIRRORS DmsRuleService.Validate
//
// Run this in the form submit handler so the user sees field-level errors
// without a network round-trip. The server ALSO enforces the same rules;
// this helper is a UX optimisation, not the source of truth.
//
// Throws `Error` (not ApiError) so callers can `try { … } catch (e)` and
// render `e.message` directly on the form.
// ──────────────────────────────────────────────────────────────────────────
export function validateDmsRuleInput(rule) {
  if (!rule || typeof rule !== "object") {
    throw new Error("Rule payload is required.");
  }

  // [min, max] pairs — kept in one place so adding a field is a one-liner.
  const bounds = {
    reminderBeforeDays:     [0, 3649],
    maxMissedCheckIns:      [1, 100],
    graceDays:              [0, 30],
    minCheckInIntervalDays: [1, 3650],
    maxCheckInIntervalDays: [1, 3650],
  };

  for (const [field, [min, max]] of Object.entries(bounds)) {
    const v = rule[field];
    if (!Number.isInteger(v) || v < min || v > max) {
      throw new Error(`${field} must be an integer in [${min}, ${max}].`);
    }
  }

  if (rule.minCheckInIntervalDays > rule.maxCheckInIntervalDays) {
    throw new Error(
      "MinCheckInIntervalDays must be less than or equal to MaxCheckInIntervalDays."
    );
  }

  if (rule.reminderBeforeDays >= rule.minCheckInIntervalDays) {
    throw new Error(
      "ReminderBeforeDays must be strictly less than MinCheckInIntervalDays."
    );
  }
}

// ──────────────────────────────────────────────────────────────────────────
// ADMIN endpoints
// ──────────────────────────────────────────────────────────────────────────

/**
 * GET /api/admin/dms-rule
 *
 * Returns the singleton rule, or `null` if the admin hasn't created one.
 * We swallow the "404 = not configured" case here so the admin page can
 * render its Create-mode form without catching. Any other error (401, 403,
 * 500) is re-thrown as ApiError for the caller to surface.
 *
 * @returns {Promise<{
 *   id: string,
 *   reminderBeforeDays: number,
 *   maxMissedCheckIns: number,
 *   graceDays: number,
 *   minCheckInIntervalDays: number,
 *   maxCheckInIntervalDays: number,
 *   updatedByAccountId: string|null,
 *   createdAt: string,
 *   updatedAt: string
 * }|null>}
 */
export async function getAdminDmsRule() {
  try {
    return await request("/admin/dms-rule");
  } catch (err) {
    if (err?.status === 404) return null;
    throw err;
  }
}

/**
 * POST /api/admin/dms-rule
 *
 * Creates the singleton rule. Possible outcomes:
 *   201 → returns the created rule
 *   400 → server-side Validate rejected (shouldn't happen if caller used
 *         validateDmsRuleInput, but the server is authoritative)
 *   409 → a rule already exists — caller should switch to PUT
 *
 * @param {Object} rule
 * @returns {Promise<Object>}
 */
export function createAdminDmsRule(rule) {
  validateDmsRuleInput(rule);
  return request("/admin/dms-rule", { method: "POST", body: rule });
}

/**
 * PUT /api/admin/dms-rule
 *
 * Updates the singleton rule in place.
 *   200 → returns the updated rule
 *   400 → cross-field validation failed
 *   404 → no rule exists — caller should switch to POST
 *
 * NOTE: existing wills are NOT retroactively re-checked. Their stored DMS
 * snapshot is only re-validated on the next ConfigureDmsAsync / SubmitAsync.
 */
export function updateAdminDmsRule(rule) {
  validateDmsRuleInput(rule);
  return request("/admin/dms-rule", { method: "PUT", body: rule });
}

/**
 * DELETE /api/admin/dms-rule
 *
 * Resets the configuration. Every owner GET /wills/dms-rule will start
 * returning 404, and ConfigureDmsAsync will throw "DMS is not available".
 * Meant for re-provisioning, not normal operation. Returns 204 (no body).
 */
export function deleteAdminDmsRule() {
  return request("/admin/dms-rule", { method: "DELETE" });
}

// ──────────────────────────────────────────────────────────────────────────
// OWNER endpoints
// ──────────────────────────────────────────────────────────────────────────

/**
 * GET /api/wills/dms-rule
 *
 * Owner-facing read. The backend (DmsRuleForOwnerResponse) DELIBERATELY
 * omits `graceDays` — never expect it here and never invent it.
 *
 * Returns `null` on 404 (rule not configured). The owner form should show
 * a disabled/"DMS unavailable — contact an administrator" banner in that
 * case rather than an error.
 *
 * @returns {Promise<{
 *   reminderBeforeDays: number,
 *   maxMissedCheckIns: number,
 *   minCheckInIntervalDays: number,
 *   maxCheckInIntervalDays: number
 * }|null>}
 */
export async function getOwnerDmsRule() {
  try {
    return await request("/wills/dms-rule");
  } catch (err) {
    if (err?.status === 404) return null;
    throw err;
  }
}

/**
 * PUT /api/wills/{willId}/dms
 *
 * Owner-level DMS configuration for a specific will. Uses the CURRENT
 * `ConfigureDmsRequest` DTO on the backend, which still carries
 * `[Range]` attributes on `reminderBeforeDays`, `maxMissedCheckIns`,
 * and `graceDays`. Those three fields are now SERVER-OVERWRITTEN from
 * the admin rule, but ASP.NET model binding validates them *before*
 * the controller runs — so we MUST send in-range values or we'll get a
 * 400 that has nothing to do with the fields the owner actually set.
 *
 * → Always pass in the effective owner rule from `getOwnerDmsRule()` so
 *   the locked fields are guaranteed in-range. For `graceDays`, the owner
 *   has no visibility into it; send 0 (in [0, 30]) and the server replaces
 *   it with the real value from the admin rule.
 *
 * Error mapping to surface in the UI:
 *   400 → message from server (usually "CheckInIntervalDays must be between
 *         X and Y days." OR "DMS is not available…" if rule was deleted)
 *   404 → will not found / not owned
 *   423 → PIN grant missing (caller must retry via the PIN modal)
 *
 * @param {string} willId
 * @param {{
 *   enabled: boolean,
 *   checkInIntervalDays: number,
 *   rule: {                                  // from getOwnerDmsRule()
 *     reminderBeforeDays: number,
 *     maxMissedCheckIns: number,
 *     minCheckInIntervalDays: number,
 *     maxCheckInIntervalDays: number
 *   }
 * }} params
 * @returns {Promise<Object>} the persisted DmsConfigurationResponse
 */
export function configureWillDms(willId, { enabled, checkInIntervalDays, rule }) {
  if (!willId) {
    throw new Error("willId is required.");
  }
  if (!rule) {
    throw new Error(
      "Owner-facing DMS rule is required to fill the locked fields."
    );
  }
  if (!Number.isInteger(checkInIntervalDays)) {
    throw new Error("CheckInIntervalDays must be an integer.");
  }
  if (
    checkInIntervalDays < rule.minCheckInIntervalDays ||
    checkInIntervalDays > rule.maxCheckInIntervalDays
  ) {
    throw new Error(
      `CheckInIntervalDays must be between ${rule.minCheckInIntervalDays} and ${rule.maxCheckInIntervalDays} days.`
    );
  }

  return request(`/wills/${encodeURIComponent(willId)}/dms`, {
    method: "PUT",
    body: {
      enabled: Boolean(enabled),
      checkInIntervalDays,

      // Sent only to satisfy the C# [Range] model validation. Server
      // overwrites all three from the effective admin rule.
      reminderBeforeDays: rule.reminderBeforeDays,
      maxMissedCheckIns: rule.maxMissedCheckIns,
      graceDays: 0, // any int in [0,30]; ignored / replaced server-side
    },
  });
}