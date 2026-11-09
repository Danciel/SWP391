-- DEMO ONLY.
-- Run only if your current MockExternalAccounts table/data is missing.
-- Current project decision intentionally keeps fake plaintext Password here.
-- NEVER use real credentials.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public."MockExternalAccounts"
(
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Provider" text NOT NULL,
    "Username" text NOT NULL,
    "Password" text NOT NULL,
    "DisplayName" text NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT "UQ_MockExternalAccounts_Provider_Username"
        UNIQUE ("Provider", "Username")
);

INSERT INTO public."MockExternalAccounts"
    ("Provider", "Username", "Password", "DisplayName", "IsActive")
VALUES
    ('GMAIL', 'demo.gmail@gmail.com', 'External@123', 'Demo Gmail Account', true),
    ('GITHUB', 'demo-github', 'External@123', 'Demo GitHub Account', true),
    ('DROPBOX', 'demo.dropbox@legacyvault.demo', 'External@123', 'Demo Dropbox Account', true),
    ('MICROSOFT', 'demo.microsoft@legacyvault.demo', 'External@123', 'Demo Microsoft Account', true),
    ('BINANCE', 'demo.binance@legacyvault.demo', 'External@123', 'Demo Binance Account', true)
ON CONFLICT ("Provider", "Username")
DO UPDATE SET
    "Password" = EXCLUDED."Password",
    "DisplayName" = EXCLUDED."DisplayName",
    "IsActive" = EXCLUDED."IsActive";
