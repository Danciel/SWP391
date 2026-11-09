-- Optional manual alternative to `dotnet ef migrations add`.
ALTER TABLE "DmsConfigurations"
  ADD COLUMN IF NOT EXISTS "GraceDays" integer NOT NULL DEFAULT 2,
  ADD COLUMN IF NOT EXISTS "LastReminderForDueAt" timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS "ThresholdReachedAt" timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS "ExecutorNotifiedAt" timestamp with time zone NULL;
