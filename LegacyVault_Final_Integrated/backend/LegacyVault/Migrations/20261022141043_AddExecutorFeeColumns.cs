using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegacyVault.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutorFeeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ExecutorProfiles",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFee",
                table: "ExecutorProfiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencySnapshot",
                table: "ExecutorAssignments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "ExecutorAssignments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "WILL");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFeeSnapshot",
                table: "ExecutorAssignments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocationPercentage",
                table: "AssetAssignments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 100m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "ServiceFee",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "CurrencySnapshot",
                table: "ExecutorAssignments");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ExecutorAssignments");

            migrationBuilder.DropColumn(
                name: "ServiceFeeSnapshot",
                table: "ExecutorAssignments");

            migrationBuilder.DropColumn(
                name: "AllocationPercentage",
                table: "AssetAssignments");
        }
    }
}
