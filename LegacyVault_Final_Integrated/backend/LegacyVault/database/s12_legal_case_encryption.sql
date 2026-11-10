-- Sprint 12 (C8): death certificates are encrypted at rest.
-- SchemaBootstrap runs the same statements at startup; this file is the manual alternative.
-- Existing (old) rows keep IsEncrypted = false and are still readable.
ALTER TABLE "LegalCases" ADD COLUMN IF NOT EXISTS "FileSha256" varchar(64) NULL;
ALTER TABLE "LegalCases" ADD COLUMN IF NOT EXISTS "IsEncrypted" boolean NOT NULL DEFAULT false;
