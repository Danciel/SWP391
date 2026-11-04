-- Sprint 10: per-recipient handover access code.
-- The code is generated when the Executor delivers, emailed to the recipient,
-- and only a salted hash is stored (nobody can read it back).
CREATE TABLE IF NOT EXISTS "HandoverAccessCodes" (
    "Id"                       uuid        NOT NULL PRIMARY KEY,
    "HandoverCaseId"           uuid        NOT NULL REFERENCES "HandoverCases" ("Id") ON DELETE CASCADE,
    "BeneficiaryDesignationId" uuid        NOT NULL REFERENCES "BeneficiaryDesignations" ("Id") ON DELETE CASCADE,
    "Salt"                     bytea       NOT NULL,
    "CodeHash"                 bytea       NOT NULL,
    "IssuedAt"                 timestamptz NOT NULL,
    "LastIssuedAt"             timestamptz NOT NULL,
    "IssueCount"               integer     NOT NULL DEFAULT 1,
    "EmailedAt"                timestamptz NULL,
    "FailedAttempts"           integer     NOT NULL DEFAULT 0,
    "LockedUntil"              timestamptz NULL,
    "VerifiedAt"               timestamptz NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_HandoverAccessCodes_Case_Designation"
    ON "HandoverAccessCodes" ("HandoverCaseId", "BeneficiaryDesignationId");
