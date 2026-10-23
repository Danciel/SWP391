-- W06: percentage allocation per WillAsset/beneficiary.
ALTER TABLE "AssetAssignments"
  ADD COLUMN IF NOT EXISTS "AllocationPercentage" numeric(5,2) NOT NULL DEFAULT 100;

-- Backfill existing multi-beneficiary assets to an equal split that totals exactly 100%.
WITH groups_to_backfill AS (
  SELECT "WillAssetId"
  FROM "AssetAssignments"
  GROUP BY "WillAssetId"
  HAVING count(*) > 1 AND bool_and("AllocationPercentage" = 100)
), ranked AS (
  SELECT aa."Id",
         row_number() OVER (PARTITION BY aa."WillAssetId" ORDER BY aa."CreatedAt", aa."Id") AS rn,
         count(*) OVER (PARTITION BY aa."WillAssetId") AS cnt
  FROM "AssetAssignments" aa
  INNER JOIN groups_to_backfill g ON g."WillAssetId" = aa."WillAssetId"
)
UPDATE "AssetAssignments" AS aa
SET "AllocationPercentage" = CASE
  WHEN ranked.rn = ranked.cnt THEN 100.00 - round(100.00 / ranked.cnt, 2) * (ranked.cnt - 1)
  ELSE round(100.00 / ranked.cnt, 2)
END
FROM ranked
WHERE aa."Id" = ranked."Id";

-- W08 / W07: Executor advertises a fee for the whole Will; assignment stores an immutable snapshot.
ALTER TABLE "ExecutorProfiles"
  ADD COLUMN IF NOT EXISTS "ServiceFee" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "Currency" character varying(3) NOT NULL DEFAULT 'VND';

ALTER TABLE "ExecutorAssignments"
  ADD COLUMN IF NOT EXISTS "ServiceFeeSnapshot" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS "CurrencySnapshot" character varying(3) NOT NULL DEFAULT 'VND',
  ADD COLUMN IF NOT EXISTS "Scope" character varying(16) NOT NULL DEFAULT 'WILL';

-- Prevent invalid individual shares. Total-per-asset validation is enforced in the service.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_AssetAssignments_AllocationPercentage') THEN
    ALTER TABLE "AssetAssignments"
      ADD CONSTRAINT "CK_AssetAssignments_AllocationPercentage"
      CHECK ("AllocationPercentage" > 0 AND "AllocationPercentage" <= 100);
  END IF;
END $$;
