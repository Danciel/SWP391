-- Executor per asset (optional specialist). Idempotent.
-- Matches Models/WillAsset.cs. Use this OR an EF migration, not both.
ALTER TABLE "WillAssets"
  ADD COLUMN IF NOT EXISTS "ExecutorAccountId" uuid NULL;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_WillAssets_Accounts_ExecutorAccountId') THEN
    ALTER TABLE "WillAssets"
      ADD CONSTRAINT "FK_WillAssets_Accounts_ExecutorAccountId"
      FOREIGN KEY ("ExecutorAccountId") REFERENCES "Accounts" ("Id") ON DELETE SET NULL;
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS "IX_WillAssets_ExecutorAccountId"
  ON "WillAssets" ("ExecutorAccountId");
