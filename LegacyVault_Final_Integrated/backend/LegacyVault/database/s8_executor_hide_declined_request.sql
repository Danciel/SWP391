-- Sprint 8: Executor can delete (hide) a declined request from their dashboard
ALTER TABLE "ExecutorAssignments"
    ADD COLUMN IF NOT EXISTS "HiddenByExecutor" boolean NOT NULL DEFAULT false;
