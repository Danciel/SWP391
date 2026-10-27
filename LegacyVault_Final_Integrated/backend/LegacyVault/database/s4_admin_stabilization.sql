-- Sprint 4 schema additions for Admin oversight, audit trail and idempotent mock top-ups.
-- Run only AFTER the Sprint 3 tables (ExecutorProfiles, WalletTransactions) exist.
-- If you generate/apply the EF Core migration AddSprint4AdminStabilization instead,
-- do NOT run this script as well.

ALTER TABLE "ExecutorProfiles"
    ADD COLUMN IF NOT EXISTS "IsSuspended" boolean NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS "SuspensionReason" varchar(500) NULL,
    ADD COLUMN IF NOT EXISTS "SuspendedAt" timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS "SuspendedByAccountId" uuid NULL;

ALTER TABLE "WalletTransactions"
    ADD COLUMN IF NOT EXISTS "IdempotencyKey" varchar(100) NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_WalletTransactions_AccountId_IdempotencyKey"
    ON "WalletTransactions" ("AccountId", "IdempotencyKey")
    WHERE "IdempotencyKey" IS NOT NULL;

CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ActorAccountId" uuid NULL,
    "Action" varchar(100) NOT NULL,
    "EntityType" varchar(100) NOT NULL,
    "EntityId" uuid NULL,
    "Details" varchar(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAt"
    ON "AuditLogs" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_ActorAccountId_CreatedAt"
    ON "AuditLogs" ("ActorAccountId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityType_EntityId_CreatedAt"
    ON "AuditLogs" ("EntityType", "EntityId", "CreatedAt");


CREATE TABLE IF NOT EXISTS "PaymentDisputes" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "WillId" uuid NOT NULL REFERENCES "Wills" ("Id") ON DELETE CASCADE,
    "RaisedByAccountId" uuid NOT NULL,
    "Reason" varchar(1000) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'OPEN',
    "ResolutionAction" varchar(20) NULL,
    "ResolutionNote" varchar(1000) NULL,
    "ResolvedByAccountId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ResolvedAt" timestamp with time zone NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentDisputes_WillId"
    ON "PaymentDisputes" ("WillId");
CREATE INDEX IF NOT EXISTS "IX_PaymentDisputes_Status"
    ON "PaymentDisputes" ("Status");
CREATE INDEX IF NOT EXISTS "IX_PaymentDisputes_RaisedByAccountId_CreatedAt"
    ON "PaymentDisputes" ("RaisedByAccountId", "CreatedAt");
