using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP301BETASK.Migrations
{
    /// <inheritdoc />
    public partial class AddWillModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wills_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryDesignations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryDesignations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeneficiaryDesignations_Accounts_BeneficiaryAccountId",
                        column: x => x.BeneficiaryAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BeneficiaryDesignations_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DmsConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CheckInIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    ReminderBeforeDays = table.Column<int>(type: "integer", nullable: false),
                    MaxMissedCheckIns = table.Column<int>(type: "integer", nullable: false),
                    MissedCheckIns = table.Column<int>(type: "integer", nullable: false),
                    LastCheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextCheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DmsConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DmsConfigurations_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutorAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutorAssignments_Accounts_ExecutorAccountId",
                        column: x => x.ExecutorAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutorAssignments_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WillAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WillAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WillAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WillAssets_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DmsCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DmsConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DmsCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DmsCheckIns_DmsConfigurations_DmsConfigurationId",
                        column: x => x.DmsConfigurationId,
                        principalTable: "DmsConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryDesignationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_BeneficiaryDesignations_BeneficiaryDesigna~",
                        column: x => x.BeneficiaryDesignationId,
                        principalTable: "BeneficiaryDesignations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_WillAssets_WillAssetId",
                        column: x => x.WillAssetId,
                        principalTable: "WillAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_BeneficiaryDesignationId",
                table: "AssetAssignments",
                column: "BeneficiaryDesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_WillAssetId_BeneficiaryDesignationId",
                table: "AssetAssignments",
                columns: new[] { "WillAssetId", "BeneficiaryDesignationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDesignations_BeneficiaryAccountId",
                table: "BeneficiaryDesignations",
                column: "BeneficiaryAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDesignations_WillId_Email",
                table: "BeneficiaryDesignations",
                columns: new[] { "WillId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DmsCheckIns_DmsConfigurationId",
                table: "DmsCheckIns",
                column: "DmsConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_DmsConfigurations_WillId",
                table: "DmsConfigurations",
                column: "WillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorAssignments_ExecutorAccountId",
                table: "ExecutorAssignments",
                column: "ExecutorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorAssignments_WillId",
                table: "ExecutorAssignments",
                column: "WillId");

            migrationBuilder.CreateIndex(
                name: "IX_WillAssets_AssetId",
                table: "WillAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_WillAssets_WillId_AssetId",
                table: "WillAssets",
                columns: new[] { "WillId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wills_OwnerAccountId",
                table: "Wills",
                column: "OwnerAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "DmsCheckIns");

            migrationBuilder.DropTable(
                name: "ExecutorAssignments");

            migrationBuilder.DropTable(
                name: "BeneficiaryDesignations");

            migrationBuilder.DropTable(
                name: "WillAssets");

            migrationBuilder.DropTable(
                name: "DmsConfigurations");

            migrationBuilder.DropTable(
                name: "Wills");
        }
    }
}
