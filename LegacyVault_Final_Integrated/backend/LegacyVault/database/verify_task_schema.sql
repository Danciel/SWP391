-- LegacyVault task schema verification - READ ONLY.
-- Safe for Supabase SQL Editor.

-- 1) Expected tables
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN (
    'Accounts',
    'EmailOtps',
    'ExecutorProfiles',
    'Assets',
    'AssetSecrets',
    'AssetVerifications',
    'MockExternalAccounts',
    'Wills',
    'WillAssets',
    'BeneficiaryDesignations',
    'AssetAssignments',
    'ExecutorAssignments',
    'DmsConfigurations',
    'DmsCheckIns'
  )
ORDER BY table_name;

-- 2) Asset columns
SELECT table_name, column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN (
    'Assets',
    'AssetSecrets',
    'AssetVerifications',
    'MockExternalAccounts'
  )
ORDER BY table_name, ordinal_position;

-- 3) Will columns
SELECT table_name, column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN (
    'Wills',
    'WillAssets',
    'BeneficiaryDesignations',
    'AssetAssignments',
    'ExecutorAssignments',
    'DmsConfigurations',
    'DmsCheckIns',
    'ExecutorProfiles'
  )
ORDER BY table_name, ordinal_position;

-- 4) Safe seed check: no hashes/passwords/secrets selected
SELECT "Id", "FullName", "Email", "Role", "IdentityStatus"
FROM public."Accounts"
ORDER BY "Role", "FullName";

SELECT
    ep."AccountId",
    a."FullName",
    ep."Rating",
    ep."CompletedCases",
    ep."ExperienceYears",
    ep."IsVerified"
FROM public."ExecutorProfiles" ep
JOIN public."Accounts" a
    ON a."Id" = ep."AccountId"
ORDER BY ep."Rating" DESC;

SELECT
    "Provider",
    "Username",
    "DisplayName",
    "IsActive"
FROM public."MockExternalAccounts"
ORDER BY "Provider", "Username";
