-- Sprint 7: Executor evaluation (from CSV) + configurable suggested-fee rules
ALTER TABLE "ExecutorProfiles"
    ADD COLUMN IF NOT EXISTS "EvalExperienceYears" integer NULL,
    ADD COLUMN IF NOT EXISTS "EvalRating" numeric(3,2) NULL,
    ADD COLUMN IF NOT EXISTS "EvalTotalCases" integer NULL,
    ADD COLUMN IF NOT EXISTS "EvalWonCases" integer NULL,
    ADD COLUMN IF NOT EXISTS "EvalSource" varchar(20) NULL,
    ADD COLUMN IF NOT EXISTS "EvalUpdatedAt" timestamptz NULL;

CREATE TABLE IF NOT EXISTS "ExecutorPricingRules" (
    "Id" uuid PRIMARY KEY,
    "BaseFee" numeric(18,2) NOT NULL,
    "PerExperienceYear" numeric(18,2) NOT NULL,
    "PerStar" numeric(18,2) NOT NULL,
    "PerCompletedCase" numeric(18,2) NOT NULL,
    "WinRateBonus" numeric(18,2) NOT NULL,
    "MinFee" numeric(18,2) NOT NULL,
    "MaxFee" numeric(18,2) NOT NULL,
    "RoundTo" numeric(18,2) NOT NULL,
    "Currency" varchar(3) NOT NULL DEFAULT 'VND',
    "UpdatedAt" timestamptz NOT NULL,
    "UpdatedByAccountId" uuid NULL
);
-- No seed row needed: the API falls back to built-in defaults until an Admin saves rules.
