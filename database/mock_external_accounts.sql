-- LegacyVault mock provider database (DEMO ONLY)
-- Password values below are fake plaintext credentials for classroom/demo verification.

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
    CONSTRAINT "UQ_MockExternalAccounts_Provider_Username" UNIQUE ("Provider", "Username")
);

INSERT INTO public."MockExternalAccounts"
    ("Provider", "Username", "Password", "DisplayName", "IsActive")
VALUES
    ('GMAIL',  'demo@gmail.com',   '123456',    'Demo Gmail Account', true),
    ('GMAIL',  'test@gmail.com',   'abc123',    'Test Gmail Account', true),
    ('GMAIL',  'legacy@gmail.com', 'legacy123', 'Legacy Gmail Account', true),
    ('GITHUB', 'giakiet',          'github123', 'GitHub Demo Account', true),
    ('GITHUB', 'developer01',      'dev123',    'Developer GitHub Account', true)
ON CONFLICT ("Provider", "Username") DO NOTHING;

SELECT * FROM public."MockExternalAccounts" ORDER BY "Provider", "Username";
