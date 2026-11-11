using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegacyVault.Migrations
{
    /// <inheritdoc />
    public partial class AddDmsRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "DmsRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderBeforeDays = table.Column<int>(type: "integer", nullable: false),
                    MaxMissedCheckIns = table.Column<int>(type: "integer", nullable: false),
                    GraceDays = table.Column<int>(type: "integer", nullable: false),
                    MinCheckInIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    MaxCheckInIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DmsRules", x => x.Id);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WillAssets_Accounts_ExecutorAccountId",
                table: "WillAssets");

            migrationBuilder.DropTable(
                name: "DmsRules");

            migrationBuilder.DropTable(
                name: "ExecutorCsvSubmissions");

            migrationBuilder.DropTable(
                name: "ExecutorDeposits");

            migrationBuilder.DropTable(
                name: "ExecutorPricingRules");

            migrationBuilder.DropTable(
                name: "HandoverAccessCodes");

            migrationBuilder.DropTable(
                name: "HandoverSignatures");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "HandoverMinutes");

            migrationBuilder.DropIndex(
                name: "IX_WillAssets_ExecutorAccountId",
                table: "WillAssets");

            migrationBuilder.DropColumn(
                name: "ExecutorAccountId",
                table: "WillAssets");

            migrationBuilder.DropColumn(
                name: "CertificateData",
                table: "LegalCases");

            migrationBuilder.DropColumn(
                name: "FileSha256",
                table: "LegalCases");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "LegalCases");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "HandoverItems");

            migrationBuilder.DropColumn(
                name: "ClaimedByAccountId",
                table: "HandoverItems");

            migrationBuilder.DropColumn(
                name: "EvalExperienceYears",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "EvalRating",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "EvalSource",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "EvalTotalCases",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "EvalUpdatedAt",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "EvalWonCases",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "TotalHeldDeposit",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "HiddenByExecutor",
                table: "ExecutorAssignments");
        }
    }
}
