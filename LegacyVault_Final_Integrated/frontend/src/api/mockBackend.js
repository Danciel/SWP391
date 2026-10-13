import { ApiError } from "./client";
import { getWillSync, getAssetsSync, upsertInheritedRecord, resolveImmediateHandover, resolveExecutorHiring } from "./mockVault";


const USERS_KEY = "lv_mock_users";
const OTP_KEY = "lv_mock_otps";
const RESET_TOKENS_KEY = "lv_mock_reset_tokens";
const FIXED_OTP = "123456"; 
const OTP_TTL_MS = 10 * 60 * 1000;
const RESET_TOKEN_TTL_MS = 15 * 60 * 1000;
const NETWORK_DELAY_MS = 500;

function delay(ms = NETWORK_DELAY_MS) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function readJSON(key, fallback) {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch {
    return fallback;
  }
}

function writeJSON(key, value) {
  localStorage.setItem(key, JSON.stringify(value));
}

function getUsers() {
  const users = readJSON(USERS_KEY, []);
  if (users.length === 0) {
    const seeded = [
      { id: "user_owner_seed", fullName: "Nguyen Van A", email: "owner@test.com", phoneNumber: "", password: "password123", role: "OWNER", verified: true, createdAt: new Date().toISOString() },
      { id: "user_executor_seed", fullName: "Digital Executor", email: "executor@test.com", phoneNumber: "", password: "password123", role: "EXECUTOR", verified: true, createdAt: new Date().toISOString() },
      { id: "user_verifier_seed", fullName: "Pham Nguyen Verifier", email: "verifier@test.com", phoneNumber: "", password: "password123", role: "LEGAL_VERIFIER", verified: true, createdAt: new Date().toISOString() },
      { id: "user_admin_seed", fullName: "System Admin", email: "admin@test.com", phoneNumber: "", password: "password123", role: "ADMIN", verified: true, createdAt: new Date().toISOString() },
    ];
    writeJSON(USERS_KEY, seeded);
    return seeded;
  }
  return users;
}

function saveUsers(users) {
  writeJSON(USERS_KEY, users);
}

function findUserByEmail(email) {
  return getUsers().find((u) => u.email.toLowerCase() === String(email || "").toLowerCase());
}

// Real registered-account lookups, used internally so other flows (Dead
// Man's Switch triggers, inheritance delivery) reference actual accounts
// instead of hardcoded demo names.
export function getRegisteredUserById(userId) {
  const user = getUsers().find((u) => u.id === userId);
  return user ? publicUser(user) : null;
}

export function findRegisteredUserIdByEmail(email) {
  const user = findUserByEmail(email);
  return user ? user.id : null;
}

function randomId(prefix = "id") {
  return `${prefix}_${Math.random().toString(36).slice(2, 10)}`;
}

function makeFakeToken(user) {
  const header = btoa(JSON.stringify({ alg: "mock", typ: "JWT" }));
  const payload = btoa(
    JSON.stringify({ sub: user.id, email: user.email, iat: Date.now() })
  );
  const signature = btoa(`mock-signature-${Math.random().toString(36).slice(2)}`);
  return `${header}.${payload}.${signature}`;
}

function publicUser(user) {
  const { password, ...rest } = user;
  return rest;
}

function setOtp(email, purpose, otp) {
  const otps = readJSON(OTP_KEY, {});
  otps[`${email.toLowerCase()}:${purpose}`] = {
    otp,
    expiresAt: Date.now() + OTP_TTL_MS,
  };
  writeJSON(OTP_KEY, otps);
}

function getOtp(email, purpose) {
  const otps = readJSON(OTP_KEY, {});
  return otps[`${email.toLowerCase()}:${purpose}`];
}

function clearOtp(email, purpose) {
  const otps = readJSON(OTP_KEY, {});
  delete otps[`${email.toLowerCase()}:${purpose}`];
  writeJSON(OTP_KEY, otps);
}

function issueResetToken(email) {
  const tokens = readJSON(RESET_TOKENS_KEY, {});
  const token = randomId("reset");
  tokens[token] = { email: email.toLowerCase(), expiresAt: Date.now() + RESET_TOKEN_TTL_MS };
  writeJSON(RESET_TOKENS_KEY, tokens);
  return token;
}

function consumeResetToken(token) {
  const tokens = readJSON(RESET_TOKENS_KEY, {});
  const entry = tokens[token];
  if (entry) {
    delete tokens[token];
    writeJSON(RESET_TOKENS_KEY, tokens);
  }
  return entry;
}

function issueOtp(email, purpose) {
  const otp = String(Math.floor(100000 + Math.random() * 900000));
  setOtp(email, purpose, otp);
  console.info(
    `%c[mock] OTP for ${email} (${purpose}): ${otp} (or just use ${FIXED_OTP})`,
    "color:#4f46e5;font-weight:bold;"
  );
  return otp;
}


export async function registerOwner({ fullName, email, phoneNumber, password }) {
  await delay();

  if (!email || !password || !fullName) {
    throw new ApiError("Missing required fields", 400);
  }
  if (findUserByEmail(email)) {
    throw new ApiError("An account with this email already exists", 409);
  }

  const user = {
    id: randomId("user"),
    fullName,
    email,
    phoneNumber,
    password,
    role: "OWNER",
    verified: false,
    createdAt: new Date().toISOString(),
  };
  const users = getUsers();
  users.push(user);
  saveUsers(users);

  issueOtp(email, "register");

  return { user: publicUser(user) };
}

export async function login({ email, password }) {
  await delay();

  const user = findUserByEmail(email);
  if (!user || user.password !== password) {
    throw new ApiError("Incorrect email or password", 401);
  }
  if (!user.verified) {
    throw new ApiError("Please verify your account via the OTP code first", 403);
  }

  const token = makeFakeToken(user);
  return { token, user: publicUser(user) };
}

export async function verifyOtp({ email, otp, purpose = "register" }) {
  await delay();

  const entry = getOtp(email, purpose);
  const isValid =
    otp === FIXED_OTP || (entry && entry.otp === otp && entry.expiresAt > Date.now());

  if (!isValid) {
    throw new ApiError("Invalid or expired OTP code", 400);
  }
  clearOtp(email, purpose);

  if (purpose === "register") {
    const users = getUsers();
    const user = users.find((u) => u.email.toLowerCase() === email.toLowerCase());
    if (!user) throw new ApiError("Account not found", 404);
    user.verified = true;
    saveUsers(users);
    return { verified: true };
  }

  if (purpose === "reset") {
    const user = findUserByEmail(email);
    if (!user) throw new ApiError("Account not found", 404);
    const resetToken = issueResetToken(email);
    return { resetToken };
  }

  return {};
}

export async function resendOtp({ email, purpose = "register" }) {
  await delay(300);
  issueOtp(email, purpose);
  return {};
}

export async function requestPasswordReset({ email }) {
  await delay();
  const user = findUserByEmail(email);
  if (user) {
    issueOtp(email, "reset");
  } else {
    console.info(`[mock] Password reset requested for unknown email: ${email}`);
  }
  return {};
}

export async function resetPassword({ email, resetToken, newPassword }) {
  await delay();

  const entry = consumeResetToken(resetToken);
  if (!entry || entry.email !== String(email || "").toLowerCase() || entry.expiresAt < Date.now()) {
    throw new ApiError("Reset link is invalid or has expired, please request a new code", 400);
  }

  const users = getUsers();
  const user = users.find((u) => u.email.toLowerCase() === email.toLowerCase());
  if (!user) throw new ApiError("Account not found", 404);
  user.password = newPassword;
  saveUsers(users);

  return {};
}


const EXEC_REQUESTS_KEY = "lv_mock_exec_requests";
const EXEC_ACTIVE_WILLS_KEY = "lv_mock_exec_active_wills";
const EXEC_DMS_TRIGGERS_KEY = "lv_mock_exec_dms_triggers";
const EXEC_CASES_KEY = "lv_mock_exec_cases";
const EXEC_FEE_KEY = "lv_mock_executor_fee";
const EXEC_HANDOVER_REQUESTS_KEY = "lv_mock_exec_handover_requests";
const CASE_DEADLINE_DAYS = 14;
// How long Executors/Legal Verifiers have to respond to a request sent to
// them (hiring requests, immediate-handover confirmations, case reviews).
const RESPONSE_DEADLINE_DAYS = 7;
const VERIFIER_CASES_KEY = "lv_mock_verifier_cases";
const ADMIN_ACCOUNTS_KEY = "lv_mock_admin_accounts";
const ADMIN_PAYMENTS_KEY = "lv_mock_admin_payments";
const ADMIN_AUDIT_LOG_KEY = "lv_mock_admin_audit_log";

function seedIfEmpty(key, seedFn) {
  const existing = readJSON(key, null);
  if (existing) return existing;
  const seeded = seedFn();
  writeJSON(key, seeded);
  return seeded;
}

function pushAuditLog(actor, action) {
  const log = readJSON(ADMIN_AUDIT_LOG_KEY, []);
  log.unshift({
    id: randomId("log"),
    time: new Date().toISOString(),
    actor,
    action,
  });
  writeJSON(ADMIN_AUDIT_LOG_KEY, log.slice(0, 200));
}

function daysAgo(n) {
  return new Date(Date.now() - n * 24 * 60 * 60 * 1000).toISOString();
}

function daysFromNow(n) {
  return new Date(Date.now() + n * 24 * 60 * 60 * 1000).toISOString();
}


// Called by mockVault.js the moment a will's Dead Man's Switch actually
// trips (missedCount >= maxMissed), so the Executor side gets a real
// trigger carrying the real will id and owner name instead of only ever
// showing the hardcoded demo seed below.
export function createDmsTrigger({ willId, ownerId, ownerName }) {
  seedExecutorData();
  const triggers = readJSON(EXEC_DMS_TRIGGERS_KEY, []);
  const existing = triggers.find((t) => t.willId === willId && t.status !== "CANCELLED");
  if (existing) return existing;

  const trigger = {
    id: randomId("trig"),
    willId: willId || null,
    ownerId: ownerId || null,
    ownerName,
    triggeredAt: new Date().toISOString(),
    status: "AWAITING_CASE",
  };
  triggers.push(trigger);
  writeJSON(EXEC_DMS_TRIGGERS_KEY, triggers);
  pushAuditLog("System", `Dead Man's Switch triggered for ${ownerName}`);
  return trigger;
}

// Lets the Owner call off an in-progress handover ("I'm still active /
// Cancel handover") as long as the Legal Verifier hasn't already made a
// final decision on it.
export function cancelCaseByWillId(willId) {
  const verifierCases = readJSON(VERIFIER_CASES_KEY, []);
  const vc = verifierCases.find((x) => x.willId === willId);
  if (vc && (vc.status === "APPROVED" || vc.status === "REJECTED")) {
    return false;
  }

  const triggers = readJSON(EXEC_DMS_TRIGGERS_KEY, []);
  const trigger = triggers.find((t) => t.willId === willId && t.status !== "CANCELLED");
  if (trigger) trigger.status = "CANCELLED";
  writeJSON(EXEC_DMS_TRIGGERS_KEY, triggers);

  const cases = readJSON(EXEC_CASES_KEY, []);
  const c = cases.find((x) => x.willId === willId);
  if (c && (c.status === "OPEN" || c.status === "SUBMITTED_FOR_REVIEW")) c.status = "CANCELLED";
  writeJSON(EXEC_CASES_KEY, cases);

  if (vc && vc.status !== "APPROVED" && vc.status !== "REJECTED") {
    vc.status = "CANCELLED";
    writeJSON(VERIFIER_CASES_KEY, verifierCases);
  }

  pushAuditLog(
    "Owner",
    `Owner confirmed they are still active — handover cancelled${trigger ? ` for ${trigger.ownerName}` : ""}`
  );
  return true;
}

function seedExecutorData() {
  seedIfEmpty(EXEC_REQUESTS_KEY, () => [
    {
      id: "req_1",
      ownerName: "Nguyen Van A",
      ownerEmail: "owner@test.com",
      ownerPhone: "090 123 4567",
      sentAt: daysAgo(2),
      deadlineAt: daysFromNow(RESPONSE_DEADLINE_DAYS - 2),
      status: "DECLINED",
      willTitle: "Nguyen Family Estate",
      assetsCount: 3,
      proposedFee: 2500000,
      message: "I'd like you to manage my digital estate, mainly bank documents and one crypto wallet.",
    },
    {
      id: "req_2",
      ownerName: "Tran Thi B",
      ownerEmail: "tranthib@test.com",
      ownerPhone: "090 765 4321",
      sentAt: daysAgo(5),
      deadlineAt: daysFromNow(RESPONSE_DEADLINE_DAYS - 5),
      status: "PENDING",
      willTitle: "Tran Family Legacy",
      assetsCount: 4,
      proposedFee: 3000000,
      message: "Please review my will draft — I have property documents and social media account access to hand over.",
    },
  ]);
  seedIfEmpty(EXEC_ACTIVE_WILLS_KEY, () => [
    {
      id: "will_1",
      ownerName: "Le Van C",
      willTitle: "Family Legacy 2026",
      status: "ACTIVE",
      description: "Digital estate covering property documents and one crypto wallet.",
      assetsCount: 2,
      beneficiariesCount: 1,
      dmsStatus: "Active · check-in every 30 days",
      createdAt: daysAgo(20),
      assets: [
        {
          id: "asset_1",
          name: "Property Deed - District 1 Apartment",
          type: "Document",
          description: "Legal ownership deed for the apartment at 123 Nguyen Hue, District 1, HCMC, including the notarized copy and floor plan.",
          status: "Secured",
          beneficiaryName: "Le Thi D",
          beneficiaryEmail: "lethid@test.com",
          beneficiaryPhone: "090 111 2222",
          beneficiaryRelationship: "Daughter",
        },
        {
          id: "asset_2",
          name: "Binance Crypto Wallet",
          type: "Crypto Wallet",
          description: "Binance account holding BTC and ETH; recovery seed phrase is stored separately with the legal verifier.",
          status: "Secured",
          beneficiaryName: "Le Thi D",
          beneficiaryEmail: "lethid@test.com",
          beneficiaryPhone: "090 111 2222",
          beneficiaryRelationship: "Daughter",
        },
      ],
    },
  ]);
  seedIfEmpty(EXEC_DMS_TRIGGERS_KEY, () => [
    { id: "trig_1", ownerName: "Le Van C", triggeredAt: daysAgo(1), status: "AWAITING_CASE" },
  ]);
  seedIfEmpty(EXEC_CASES_KEY, () => []);
  seedIfEmpty(EXEC_HANDOVER_REQUESTS_KEY, () => []);
}

export async function getExecutorDashboard() {
  await delay(400);
  seedExecutorData();
  return {
    requests: readJSON(EXEC_REQUESTS_KEY, []),
    activeWills: readJSON(EXEC_ACTIVE_WILLS_KEY, []),
    dmsTriggers: readJSON(EXEC_DMS_TRIGGERS_KEY, []),
    cases: readJSON(EXEC_CASES_KEY, []),
    handoverRequests: readJSON(EXEC_HANDOVER_REQUESTS_KEY, []),
  };
}

// ---------------------------------------------------------------------------
// Executor hiring: Owner submits a will (or assigns a new executor) ->
// real request sent to the Executor with a full asset snapshot -> Executor
// accepts/declines -> Owner's real will is updated (see resolveExecutorHiring
// in mockVault.js) and, on accept, the Executor's "Active Wills" list gets
// the same real data instead of only the hardcoded demo seed.
// ---------------------------------------------------------------------------
export function createExecutorHireRequest({
  willId,
  ownerId,
  ownerName,
  ownerEmail,
  ownerPhone,
  willTitle,
  description,
  executorName,
  assetsCount,
  beneficiariesCount,
  assets,
  dmsStatus,
  proposedFee,
  createdAt,
  requestedAt,
  deadlineAt,
}) {
  seedExecutorData();
  const requests = readJSON(EXEC_REQUESTS_KEY, []);
  const request = {
    id: randomId("req"),
    willId,
    ownerId,
    ownerName,
    ownerEmail: ownerEmail || "",
    ownerPhone: ownerPhone || "",
    sentAt: requestedAt || new Date().toISOString(),
    deadlineAt: deadlineAt || daysFromNow(RESPONSE_DEADLINE_DAYS),
    status: "PENDING",
    willTitle,
    description: description || "",
    executorName,
    assetsCount: assetsCount || 0,
    beneficiariesCount: beneficiariesCount || 0,
    assets: assets || [],
    dmsStatus: dmsStatus || "—",
    proposedFee: proposedFee || 0,
    createdAt: createdAt || new Date().toISOString(),
    message: `Please review my will draft for "${willTitle}".`,
  };
  requests.push(request);
  writeJSON(EXEC_REQUESTS_KEY, requests);
  pushAuditLog("Owner", `Sent executor hiring request for will "${willTitle}"`);
  return request;
}

export async function respondToExecutorRequest({ requestId, accept }) {
  await delay(350);
  const requests = readJSON(EXEC_REQUESTS_KEY, []);
  const req = requests.find((r) => r.id === requestId);
  if (!req) throw new ApiError("Request not found", 404);
  if (req.status === "PENDING" && req.deadlineAt && new Date(req.deadlineAt) < new Date()) {
    throw new ApiError("The response deadline for this request has passed", 400);
  }
  req.status = accept ? "ACCEPTED" : "DECLINED";
  writeJSON(EXEC_REQUESTS_KEY, requests);

  if (accept) {
    const wills = readJSON(EXEC_ACTIVE_WILLS_KEY, []);
    wills.push({
      id: randomId("will"),
      ownerName: req.ownerName,
      willTitle: req.willTitle || `${req.ownerName}'s Legacy Will`,
      status: "ACTIVE",
      description: req.description || "",
      assetsCount: req.assetsCount ?? "—",
      beneficiariesCount: req.beneficiariesCount ?? "—",
      dmsStatus: req.dmsStatus || "—",
      createdAt: req.createdAt || new Date().toISOString(),
      assets: req.assets || [],
    });
    writeJSON(EXEC_ACTIVE_WILLS_KEY, wills);
  }

  // Real requests (created via createExecutorHireRequest) carry the real
  // Owner/Will ids and fold the decision back into the Owner's own will;
  // the hardcoded demo seed requests don't, so they only affect this list.
  if (req.ownerId && req.willId) {
    resolveExecutorHiring(req.ownerId, req.willId, accept);
  }

  pushAuditLog("Executor", `${accept ? "Accepted" : "Declined"} request from ${req.ownerName}`);
  return { request: req };
}

export async function openExecutorCase({ triggerId }) {
  await delay(400);
  const triggers = readJSON(EXEC_DMS_TRIGGERS_KEY, []);
  const trigger = triggers.find((t) => t.id === triggerId);
  if (!trigger) throw new ApiError("Trigger not found", 404);
  trigger.status = "CASE_OPENED";
  writeJSON(EXEC_DMS_TRIGGERS_KEY, triggers);

  const cases = readJSON(EXEC_CASES_KEY, []);
  const newCase = {
    id: randomId("case"),
    willId: trigger.willId || null,
    ownerId: trigger.ownerId || null,
    ownerName: trigger.ownerName,
    status: "OPEN",
    openedAt: new Date().toISOString(),
    deadlineAt: daysFromNow(CASE_DEADLINE_DAYS),
    documentsSubmitted: 0,
    profileVerified: false,
    ownerProfile: {
      fullName: trigger.ownerName,
      idNumber: "079" + String(Math.floor(100000000 + Math.random() * 899999999)),
      dateOfBirth: "1975-04-12",
      address: "123 Nguyen Trai, District 1, Ho Chi Minh City",
    },
  };
  cases.push(newCase);
  writeJSON(EXEC_CASES_KEY, cases);

  const verifierCases = readJSON(VERIFIER_CASES_KEY, []);
  verifierCases.push({
    id: newCase.id,
    willId: trigger.willId || null,
    ownerId: trigger.ownerId || null,
    ownerName: trigger.ownerName,
    executorName: "Digital Executor",
    executorDeclarant: "Digital Executor",
    deathCertificate: null,
    submittedAt: null,
    status: "AWAITING_DOCUMENTS",
    flagged: false,
    flagReason: null,
    flaggedAt: null,
  });
  writeJSON(VERIFIER_CASES_KEY, verifierCases);

  pushAuditLog("Executor", `Opened case for ${trigger.ownerName}`);
  return { case: newCase };
}

export async function verifyCaseProfile({ caseId }) {
  await delay(400);
  const cases = readJSON(EXEC_CASES_KEY, []);
  const c = cases.find((x) => x.id === caseId);
  if (!c) throw new ApiError("Case not found", 404);
  if (c.status !== "OPEN") {
    throw new ApiError("Case is not open for profile verification", 400);
  }
  if (c.deadlineAt && new Date(c.deadlineAt) < new Date()) {
    throw new ApiError("Verification deadline has passed for this case", 400);
  }
  c.profileVerified = true;
  c.profileVerifiedAt = new Date().toISOString();
  writeJSON(EXEC_CASES_KEY, cases);
  pushAuditLog("Executor", `Verified owner profile details for case ${caseId}`);
  return { case: c };
}

export async function submitExecutorLegalDocuments({ caseId, documentNames = [] }) {
  await delay(500);
  const cases = readJSON(EXEC_CASES_KEY, []);
  const c = cases.find((x) => x.id === caseId);
  if (!c) throw new ApiError("Case not found", 404);
  if (!c.profileVerified) {
    throw new ApiError("Verify the owner's profile details before submitting documents", 400);
  }
  c.status = "SUBMITTED_FOR_REVIEW";
  c.documentsSubmitted = documentNames.length || c.documentsSubmitted + 1;
  writeJSON(EXEC_CASES_KEY, cases);

  const verifierCases = readJSON(VERIFIER_CASES_KEY, []);
  const vc = verifierCases.find((x) => x.id === caseId);
  if (vc) {
    vc.status = "PENDING_REVIEW";
    vc.submittedAt = new Date().toISOString();
    vc.deadlineAt = daysFromNow(RESPONSE_DEADLINE_DAYS);
    vc.deathCertificate = {
      fileName: documentNames.find((n) => /death|certificate/i.test(n)) || documentNames[0] || "death_certificate.pdf",
      uploadedAt: new Date().toISOString(),
    };
  }
  writeJSON(VERIFIER_CASES_KEY, verifierCases);

  pushAuditLog("Executor", `Submitted legal documents for case ${caseId}`);
  return { case: c };
}


// Snapshot of the will's current content, shown to the Legal Verifier so they
// can re-verify it (owner, executor, assets, beneficiaries) against the
// documents the Executor submitted before approving a case.
function buildWillInfo(vc) {
  if (!vc.willId || !vc.ownerId) return null;
  const will = getWillSync(vc.ownerId, vc.willId);
  if (!will) return null;
  const ownerAssets = getAssetsSync(vc.ownerId);
  return {
    name: will.name || "Untitled will",
    description: will.description || "",
    executorName: will.executor?.name || "—",
    createdAt: will.createdAt,
    assets: (will.assets || []).map((entry) => {
      const asset = ownerAssets.find((a) => a.id === entry.assetId);
      return {
        assetId: entry.assetId,
        name: asset?.name || "Asset",
        type: asset?.type || "Document",
        beneficiaryName: entry.beneficiary?.fullName || "—",
        beneficiaryEmail: entry.beneficiary?.email || "",
        beneficiaryRelationship: entry.beneficiary?.relationship || "",
      };
    }),
  };
}

export async function getLegalVerifierDashboard() {
  await delay(400);
  seedExecutorData();
  const cases = readJSON(VERIFIER_CASES_KEY, []).map((c) => ({ ...c, willInfo: buildWillInfo(c) }));
  return { cases };
}

export async function reviewLegalCase({ caseId, approve, reason, willVerified }) {
  await delay(450);
  const cases = readJSON(VERIFIER_CASES_KEY, []);
  const c = cases.find((x) => x.id === caseId);
  if (!c) throw new ApiError("Case not found", 404);
  if (c.status !== "PENDING_REVIEW") {
    throw new ApiError("Case is not awaiting review", 400);
  }
  if (c.deadlineAt && new Date(c.deadlineAt) < new Date()) {
    throw new ApiError("The review deadline has passed for this case", 400);
  }
  if (approve && !willVerified) {
    throw new ApiError("You must re-verify the will information before approving this case", 400);
  }
  c.status = approve ? "APPROVED" : "REJECTED";
  c.reviewedAt = new Date().toISOString();
  if (approve) c.willVerifiedAt = c.reviewedAt;
  if (!approve) c.rejectionReason = reason || "No reason provided";
  writeJSON(VERIFIER_CASES_KEY, cases);

  const execCases = readJSON(EXEC_CASES_KEY, []);
  const ec = execCases.find((x) => x.id === caseId);
  if (ec) ec.status = approve ? "APPROVED" : "REJECTED";
  writeJSON(EXEC_CASES_KEY, execCases);

  if (approve) deliverInheritance(c);

  pushAuditLog("Legal Verifier", `${approve ? "Approved" : "Rejected"} case ${caseId}`);
  return { case: c };
}

// Once a case is approved (or an immediate handover is confirmed), hand each
// will asset over to its beneficiary's own inherited-wills list — grouped by
// beneficiary, and only delivered to beneficiaries who have an actual
// registered account (matched by the email the Owner assigned them under).
// `meta` lets callers other than the death-verification flow (e.g. an
// immediate/living handover) override the life-status/verification labels.
function deliverInheritance(verifierCase, meta = {}) {
  if (!verifierCase.willId || !verifierCase.ownerId) return;
  const will = getWillSync(verifierCase.ownerId, verifierCase.willId);
  if (!will) return;
  const ownerAssets = getAssetsSync(verifierCase.ownerId);

  const assetsByBeneficiaryEmail = new Map();
  (will.assets || []).forEach((entry) => {
    const beneficiary = entry.beneficiary;
    if (!beneficiary?.email) return;
    const asset = ownerAssets.find((a) => a.id === entry.assetId);
    const list = assetsByBeneficiaryEmail.get(beneficiary.email) || [];
    list.push({ id: entry.assetId, name: asset?.name || "Asset", type: asset?.type || "Document" });
    assetsByBeneficiaryEmail.set(beneficiary.email, list);
  });

  assetsByBeneficiaryEmail.forEach((assets, email) => {
    const beneficiaryUserId = findRegisteredUserIdByEmail(email);
    if (!beneficiaryUserId) return; // no matching registered account to deliver to yet
    upsertInheritedRecord(beneficiaryUserId, {
      id: `inh_${will.id}`,
      willName: will.name,
      from: verifierCase.ownerName,
      ownerLifeStatus: meta.ownerLifeStatus || "DECEASED",
      legalVerification: meta.legalVerification || "APPROVED",
      claimStatus: "READY_TO_CLAIM",
      assets,
      claimKey: null,
    });
  });

  pushAuditLog("System", `Delivered inheritance records for ${verifierCase.ownerName}'s will`);
}

// ---------------------------------------------------------------------------
// Immediate Handover: Owner voluntarily hands a will's assets to their
// Executor while still alive. Owner sends the request -> Executor is
// notified -> Executor checks the beneficiary(ies) exist/are valid and
// confirms or declines -> Owner sees the decision. Same 7-day response
// deadline as other requests sent to the Executor.
// ---------------------------------------------------------------------------
export function createHandoverRequest({
  willId,
  ownerId,
  ownerName,
  willTitle,
  executorName,
  assets,
  requestedAt,
  deadlineAt,
}) {
  seedExecutorData();
  const requests = readJSON(EXEC_HANDOVER_REQUESTS_KEY, []);
  const request = {
    id: randomId("handover"),
    willId,
    ownerId,
    ownerName,
    willTitle,
    executorName,
    assets: assets || [],
    requestedAt: requestedAt || new Date().toISOString(),
    deadlineAt: deadlineAt || daysFromNow(RESPONSE_DEADLINE_DAYS),
    status: "PENDING",
    respondedAt: null,
  };
  requests.push(request);
  writeJSON(EXEC_HANDOVER_REQUESTS_KEY, requests);
  pushAuditLog("Owner", `Requested immediate handover for will "${willTitle}"`);
  return request;
}

export async function respondToHandoverRequest({ requestId, confirm }) {
  await delay(450);
  const requests = readJSON(EXEC_HANDOVER_REQUESTS_KEY, []);
  const req = requests.find((r) => r.id === requestId);
  if (!req) throw new ApiError("Handover request not found", 404);
  if (req.status !== "PENDING") {
    throw new ApiError("This handover request has already been responded to", 400);
  }
  if (req.deadlineAt && new Date(req.deadlineAt) < new Date()) {
    throw new ApiError("The response deadline for this handover request has passed", 400);
  }

  req.status = confirm ? "CONFIRMED" : "DECLINED";
  req.respondedAt = new Date().toISOString();
  writeJSON(EXEC_HANDOVER_REQUESTS_KEY, requests);

  resolveImmediateHandover(req.ownerId, req.willId, confirm);

  if (confirm) {
    deliverInheritance(
      { willId: req.willId, ownerId: req.ownerId, ownerName: req.ownerName },
      { ownerLifeStatus: "ALIVE", legalVerification: "NOT_REQUIRED_IMMEDIATE_HANDOVER" }
    );
  }

  pushAuditLog(
    "Executor",
    `${confirm ? "Confirmed" : "Declined"} immediate handover request from ${req.ownerName}`
  );
  return { request: req };
}

export async function flagSuspectedFraud({ caseId, reason }) {
  await delay(450);
  const cases = readJSON(VERIFIER_CASES_KEY, []);
  const c = cases.find((x) => x.id === caseId);
  if (!c) throw new ApiError("Case not found", 404);
  c.flagged = true;
  c.flagReason = reason || "Suspected fraud";
  c.flaggedAt = new Date().toISOString();
  writeJSON(VERIFIER_CASES_KEY, cases);

  pushAuditLog("Legal Verifier", `Flagged case ${caseId} as suspected fraud`);
  return { case: c };
}


function seedAdminData() {
  seedIfEmpty(ADMIN_ACCOUNTS_KEY, () => [
    { id: "acc_1", name: "Nguyen Van A", role: "OWNER", status: "ACTIVE" },
    { id: "acc_2", name: "Digital Executor", role: "EXECUTOR", status: "ACTIVE" },
    { id: "acc_3", name: "Pham Thi Verifier", role: "LEGAL_VERIFIER", status: "ACTIVE" },
    { id: "acc_4", name: "Tran Thi B", role: "OWNER", status: "ACTIVE" },
  ]);
  seedIfEmpty(ADMIN_PAYMENTS_KEY, () => [
    { id: "pay_1", user: "Nguyen Van A", plan: "Premium Vault", amount: 29.99, date: daysAgo(3), status: "PAID" },
    { id: "pay_2", user: "Tran Thi B", plan: "Standard Vault", amount: 9.99, date: daysAgo(10), status: "PAID" },
  ]);
  seedIfEmpty(ADMIN_AUDIT_LOG_KEY, () => [
    { id: "log_seed", time: daysAgo(1), actor: "System", action: "System initialized" },
  ]);
}

export async function getAdminDashboard() {
  await delay(400);
  seedAdminData();
  seedExecutorData();
  const flaggedCases = readJSON(VERIFIER_CASES_KEY, []).filter((c) => c.flagged);
  return {
    accounts: readJSON(ADMIN_ACCOUNTS_KEY, []),
    cases: flaggedCases,
    payments: readJSON(ADMIN_PAYMENTS_KEY, []),
    auditLog: readJSON(ADMIN_AUDIT_LOG_KEY, []),
  };
}

export async function setAccountStatus({ accountId, status }) {
  await delay(350);
  const accounts = readJSON(ADMIN_ACCOUNTS_KEY, []);
  const acc = accounts.find((a) => a.id === accountId);
  if (!acc) throw new ApiError("Account not found", 404);
  acc.status = status;
  writeJSON(ADMIN_ACCOUNTS_KEY, accounts);
  pushAuditLog("Admin", `Set account ${acc.name} to ${status}`);
  return { account: acc };
}

const VALID_ROLES = ["OWNER", "EXECUTOR", "LEGAL_VERIFIER", "ADMIN"];

export async function setAccountRole({ accountId, role }) {
  await delay(350);
  if (!VALID_ROLES.includes(role)) {
    throw new ApiError("Invalid role", 400);
  }
  const accounts = readJSON(ADMIN_ACCOUNTS_KEY, []);
  const acc = accounts.find((a) => a.id === accountId);
  if (!acc) throw new ApiError("Account not found", 404);
  const previousRole = acc.role;
  acc.role = role;
  writeJSON(ADMIN_ACCOUNTS_KEY, accounts);
  pushAuditLog("Admin", `Changed ${acc.name}'s role from ${previousRole} to ${role}`);
  return { account: acc };
}

const DEFAULT_EXECUTOR_FEE = 3000000;

export async function getExecutorFee() {
  await delay(300);
  const stored = readJSON(EXEC_FEE_KEY, null);
  return { fee: stored?.fee ?? DEFAULT_EXECUTOR_FEE, updatedAt: stored?.updatedAt ?? null };
}

export async function setExecutorFee({ fee }) {
  await delay(400);
  const numericFee = Number(fee);
  if (!Number.isFinite(numericFee) || numericFee < 0) {
    throw new ApiError("Fee must be a valid non-negative number", 400);
  }
  const entry = { fee: numericFee, updatedAt: new Date().toISOString() };
  writeJSON(EXEC_FEE_KEY, entry);
  pushAuditLog("Executor", `Updated service fee to ${numericFee.toLocaleString("vi-VN")} VND`);
  return entry;
}

