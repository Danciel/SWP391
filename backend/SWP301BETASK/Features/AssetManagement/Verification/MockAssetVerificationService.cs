using Microsoft.EntityFrameworkCore;
using Npgsql;
using SWP301BETASK.Data;
using SWP301BETASK.Features.AssetManagement.Contracts;

namespace SWP301BETASK.Features.AssetManagement.Verification
{
    public class MockAssetVerificationService
        : IAssetVerificationService
    {
        private readonly LegacyVaultDbContext _db;

        public MockAssetVerificationService(
            LegacyVaultDbContext db)
        {
            _db = db;
        }

        public async Task<bool> VerifyAsync(
            string provider,
            string username,
            string password)
        {
            var connectionString =
                _db.Database.GetConnectionString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string was not found.");
            }

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM public."MockExternalAccounts"
                    WHERE "Provider" = @provider
                      AND "Username" = @username
                      AND "PasswordHash"
                          = crypt(@password, "PasswordHash")
                      AND "IsActive" = true
                );
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "provider",
                provider.Trim().ToUpperInvariant());

            command.Parameters.AddWithValue(
                "username",
                username.Trim());

            command.Parameters.AddWithValue(
                "password",
                password);

            var result =
                await command.ExecuteScalarAsync();

            return result != null
                   && Convert.ToBoolean(result);
        }
    }
}