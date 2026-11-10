-- Sprint 11 (C1): handover minutes (bien ban) + required signatures.
-- SchemaBootstrap runs the same statements at startup; this file is the
-- manual alternative. Safe to run more than once.
CREATE TABLE IF NOT EXISTS "HandoverMinutes" (
    "Id"            uuid        NOT NULL PRIMARY KEY,
    "HandoverCaseId" uuid       NOT NULL REFERENCES "HandoverCases" ("Id") ON DELETE CASCADE,
    "WillId"        uuid        NOT NULL,
    "Version"       integer     NOT NULL DEFAULT 1,
    "Status"        text        NOT NULL,   -- PENDING_SIGNATURES | FULLY_SIGNED | VOID
    "ContentJson"   text        NOT NULL,
    "ContentSha256" varchar(64) NOT NULL,
    "PdfContent"    bytea       NOT NULL,
    "PdfSha256"     varchar(64) NOT NULL,
    "CreatedAt"     timestamptz NOT NULL,
    "FullySignedAt" timestamptz NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_HandoverMinutes_HandoverCaseId"
    ON "HandoverMinutes" ("HandoverCaseId");
CREATE INDEX IF NOT EXISTS "IX_HandoverMinutes_WillId"
    ON "HandoverMinutes" ("WillId");

CREATE TABLE IF NOT EXISTS "HandoverSignatures" (
    "Id"                       uuid        NOT NULL PRIMARY KEY,
    "MinutesId"                uuid        NOT NULL REFERENCES "HandoverMinutes" ("Id") ON DELETE CASCADE,
    "Role"                     text        NOT NULL,   -- EXECUTOR | BENEFICIARY
    "BeneficiaryDesignationId" uuid        NULL,
    "SignerAccountId"          uuid        NULL,
    "SignerName"               text        NOT NULL,
    "Status"                   text        NOT NULL,   -- PENDING | SIGNED
    "SignedAt"                 timestamptz NULL,
    "SignedByAccountId"        uuid        NULL,
    "SignedContentSha256"      varchar(64) NULL,
    "SignatureHash"            varchar(64) NULL,
    "IpAddress"                text        NULL
);

CREATE INDEX IF NOT EXISTS "IX_HandoverSignatures_MinutesId"
    ON "HandoverSignatures" ("MinutesId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_HandoverSignatures_Executor"
    ON "HandoverSignatures" ("MinutesId") WHERE "Role" = 'EXECUTOR';
CREATE UNIQUE INDEX IF NOT EXISTS "IX_HandoverSignatures_Beneficiary"
    ON "HandoverSignatures" ("MinutesId", "BeneficiaryDesignationId") WHERE "Role" = 'BENEFICIARY';
