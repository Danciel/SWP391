using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Data
{
    // Creates the small tables added after the last EF migration, so that a
    // forgotten SQL script never turns into HTTP 500 for the recipient.
    // Every statement is idempotent (IF NOT EXISTS), same as the .sql files in /database.
    public static class SchemaBootstrap
    {
        private static readonly string[] Statements =
        {
            // s10: per-recipient handover access code.
            @"CREATE TABLE IF NOT EXISTS ""HandoverAccessCodes"" (
                ""Id""                       uuid        NOT NULL PRIMARY KEY,
                ""HandoverCaseId""           uuid        NOT NULL REFERENCES ""HandoverCases"" (""Id"") ON DELETE CASCADE,
                ""BeneficiaryDesignationId"" uuid        NOT NULL REFERENCES ""BeneficiaryDesignations"" (""Id"") ON DELETE CASCADE,
                ""Salt""                     bytea       NOT NULL,
                ""CodeHash""                 bytea       NOT NULL,
                ""IssuedAt""                 timestamptz NOT NULL,
                ""LastIssuedAt""             timestamptz NOT NULL,
                ""IssueCount""               integer     NOT NULL DEFAULT 1,
                ""EmailedAt""                timestamptz NULL,
                ""FailedAttempts""           integer     NOT NULL DEFAULT 0,
                ""LockedUntil""              timestamptz NULL,
                ""VerifiedAt""               timestamptz NULL
            )",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_HandoverAccessCodes_Case_Designation""
                ON ""HandoverAccessCodes"" (""HandoverCaseId"", ""BeneficiaryDesignationId"")",

            // s11 (C1): handover minutes + required signatures.
            @"CREATE TABLE IF NOT EXISTS ""HandoverMinutes"" (
                ""Id""            uuid         NOT NULL PRIMARY KEY,
                ""HandoverCaseId"" uuid        NOT NULL REFERENCES ""HandoverCases"" (""Id"") ON DELETE CASCADE,
                ""WillId""        uuid         NOT NULL,
                ""Version""       integer      NOT NULL DEFAULT 1,
                ""Status""        text         NOT NULL,
                ""ContentJson""   text         NOT NULL,
                ""ContentSha256"" varchar(64)  NOT NULL,
                ""PdfContent""    bytea        NOT NULL,
                ""PdfSha256""     varchar(64)  NOT NULL,
                ""CreatedAt""     timestamptz  NOT NULL,
                ""FullySignedAt"" timestamptz  NULL
            )",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_HandoverMinutes_HandoverCaseId""
                ON ""HandoverMinutes"" (""HandoverCaseId"")",
            @"CREATE INDEX IF NOT EXISTS ""IX_HandoverMinutes_WillId""
                ON ""HandoverMinutes"" (""WillId"")",
            @"CREATE TABLE IF NOT EXISTS ""HandoverSignatures"" (
                ""Id""                       uuid        NOT NULL PRIMARY KEY,
                ""MinutesId""                uuid        NOT NULL REFERENCES ""HandoverMinutes"" (""Id"") ON DELETE CASCADE,
                ""Role""                     text        NOT NULL,
                ""BeneficiaryDesignationId"" uuid        NULL,
                ""SignerAccountId""          uuid        NULL,
                ""SignerName""               text        NOT NULL,
                ""Status""                   text        NOT NULL,
                ""SignedAt""                 timestamptz NULL,
                ""SignedByAccountId""        uuid        NULL,
                ""SignedContentSha256""      varchar(64) NULL,
                ""SignatureHash""            varchar(64) NULL,
                ""IpAddress""                text        NULL
            )",
            @"CREATE INDEX IF NOT EXISTS ""IX_HandoverSignatures_MinutesId""
                ON ""HandoverSignatures"" (""MinutesId"")",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_HandoverSignatures_Executor""
                ON ""HandoverSignatures"" (""MinutesId"") WHERE ""Role"" = 'EXECUTOR'",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_HandoverSignatures_Beneficiary""
                ON ""HandoverSignatures"" (""MinutesId"", ""BeneficiaryDesignationId"") WHERE ""Role"" = 'BENEFICIARY'",

            // s12 (C8): death certificates are encrypted at rest.
            @"DO $$
              BEGIN
                  IF to_regclass('public.""LegalCases""') IS NOT NULL THEN
                      ALTER TABLE ""LegalCases"" ADD COLUMN IF NOT EXISTS ""FileSha256"" varchar(64) NULL;
                      ALTER TABLE ""LegalCases"" ADD COLUMN IF NOT EXISTS ""IsEncrypted"" boolean NOT NULL DEFAULT false;
                  END IF;
              END $$",

            // Step 4: THRESHOLD_REACHED was the old name of TRIGGERED.
            @"UPDATE ""DmsConfigurations"" SET ""Status"" = 'TRIGGERED' WHERE ""Status"" = 'THRESHOLD_REACHED'"
        };

        public static async Task EnsureAsync(IServiceProvider services, ILogger logger)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LegacyVaultDbContext>();

                foreach (var sql in Statements)
                    await db.Database.ExecuteSqlRawAsync(sql);

                logger.LogInformation("Schema bootstrap finished ({Count} statements).", Statements.Length);
            }
            catch (Exception ex)
            {
                // The app still starts; the SQL files in /database can be run by hand.
                logger.LogError(ex,
                    "Schema bootstrap failed. Run the scripts in backend/LegacyVault/database by hand.");
            }
        }
    }
}
