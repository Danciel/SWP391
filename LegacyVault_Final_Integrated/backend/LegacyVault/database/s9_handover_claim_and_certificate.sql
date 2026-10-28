-- Sprint 9: beneficiary claim (PIN + face) and structured death-certificate data
ALTER TABLE "HandoverItems"
    ADD COLUMN IF NOT EXISTS "ClaimedAt" timestamptz NULL,
    ADD COLUMN IF NOT EXISTS "ClaimedByAccountId" uuid NULL;

ALTER TABLE "LegalCases"
    ADD COLUMN IF NOT EXISTS "CertificateData" text NULL;
