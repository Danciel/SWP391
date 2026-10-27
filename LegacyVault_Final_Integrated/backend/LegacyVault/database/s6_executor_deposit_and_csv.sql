-- Sprint 6: Executor 20% security deposit + Executor CSV submission
ALTER TABLE "ExecutorProfiles"
    ADD COLUMN IF NOT EXISTS "TotalHeldDeposit" numeric(18,2) NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS "ExecutorDeposits" (
    "Id" uuid PRIMARY KEY,
    "WillId" uuid NOT NULL UNIQUE REFERENCES "Wills"("Id") ON DELETE CASCADE,
    "ExecutorAccountId" uuid NOT NULL REFERENCES "Accounts"("Id") ON DELETE RESTRICT,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" varchar(3) NOT NULL DEFAULT 'VND',
    "Status" varchar(20) NOT NULL DEFAULT 'HELD',
    "HeldAt" timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS "ExecutorCsvSubmissions" (
    "Id" uuid PRIMARY KEY,
    "ExecutorAccountId" uuid NOT NULL REFERENCES "Accounts"("Id") ON DELETE CASCADE,
    "OriginalFileName" varchar(255) NOT NULL,
    "ContentType" varchar(100) NOT NULL DEFAULT 'text/csv',
    "FileContent" bytea NOT NULL,
    "UploadedAt" timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ExecutorCsvSubmissions_UploadedAt" ON "ExecutorCsvSubmissions" ("UploadedAt");
