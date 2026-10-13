using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP301BETASK.Migrations
{
    /// <inheritdoc />
    public partial class AddWillManagementModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Specialization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    License = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    Reviews = table.Column<int>(type: "integer", nullable: false),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: false),
                    CompletedCases = table.Column<int>(type: "integer", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    Available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutorProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryDesignations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IdentityNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryDesignations", x => x.Id);
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
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    ReminderDays = table.Column<int>(type: "integer", nullable: false),
                    MaxMissed = table.Column<int>(type: "integer", nullable: false),
                    MissedCount = table.Column<int>(type: "integer", nullable: false),
                    LastCheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
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
                    ExecutorProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fee = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadlineAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutorAssignments_ExecutorProfiles_ExecutorProfileId",
                        column: x => x.ExecutorProfileId,
                        principalTable: "ExecutorProfiles",
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
                name: "AssetAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryDesignationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedByOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignmentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HandoverStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_BeneficiaryDesignations_BeneficiaryDesigna~",
                        column: x => x.BeneficiaryDesignationId,
                        principalTable: "BeneficiaryDesignations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ExecutorProfiles",
                columns: new[] { "Id", "AccountId", "Available", "CompletedCases", "ExperienceYears", "Fee", "License", "Name", "Rating", "Reviews", "Specialization" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, true, 86, 8, 3000000m, "LAW-2026-00182", "David Nguyen", 4.9000000000000004, 128, "Digital Estate Specialist" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, true, 61, 6, 2500000m, "LAW-2026-00219", "Linh Tran", 4.7999999999999998, 94, "Crypto Asset Specialist" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, true, 140, 11, 3500000m, "LAW-2025-00087", "Khoa Pham", 4.7000000000000002, 152, "Family Legacy Attorney" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_BeneficiaryDesignationId",
                table: "AssetAssignments",
                column: "BeneficiaryDesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_WillId_AssetId",
                table: "AssetAssignments",
                columns: new[] { "WillId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDesignations_BeneficiaryAccountId",
                table: "BeneficiaryDesignations",
                column: "BeneficiaryAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDesignations_WillId",
                table: "BeneficiaryDesignations",
                column: "WillId");

            migrationBuilder.CreateIndex(
                name: "IX_DmsConfigurations_WillId",
                table: "DmsConfigurations",
                column: "WillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorAssignments_ExecutorProfileId",
                table: "ExecutorAssignments",
                column: "ExecutorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorAssignments_WillId",
                table: "ExecutorAssignments",
                column: "WillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wills_OwnerId",
                table: "Wills",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "DmsConfigurations");

            migrationBuilder.DropTable(
                name: "ExecutorAssignments");

            migrationBuilder.DropTable(
                name: "BeneficiaryDesignations");

            migrationBuilder.DropTable(
                name: "ExecutorProfiles");

            migrationBuilder.DropTable(
                name: "Wills");
        }
    }
}
