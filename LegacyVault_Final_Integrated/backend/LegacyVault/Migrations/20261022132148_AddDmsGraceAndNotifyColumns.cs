using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegacyVault.Migrations
{
    /// <inheritdoc />
    public partial class AddDmsGraceAndNotifyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExecutorNotifiedAt",
                table: "DmsConfigurations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GraceDays",
                table: "DmsConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReminderForDueAt",
                table: "DmsConfigurations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThresholdReachedAt",
                table: "DmsConfigurations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutorNotifiedAt",
                table: "DmsConfigurations");

            migrationBuilder.DropColumn(
                name: "GraceDays",
                table: "DmsConfigurations");

            migrationBuilder.DropColumn(
                name: "LastReminderForDueAt",
                table: "DmsConfigurations");

            migrationBuilder.DropColumn(
                name: "ThresholdReachedAt",
                table: "DmsConfigurations");
        }
    }
}
