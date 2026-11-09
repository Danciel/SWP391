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
