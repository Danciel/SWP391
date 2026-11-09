using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegacyVault.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverLegalWalletReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "ExecutorProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "BeneficiaryDesignations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExecutorReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutorReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutorReviews_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoverCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InitiatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverCases_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegalCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalCases_Accounts_ExecutorAccountId",
                        column: x => x.ExecutorAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalCases_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HeldAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePayments_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HandoverItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandoverCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverItems_AssetAssignments_AssetAssignmentId",
                        column: x => x.AssetAssignmentId,
                        principalTable: "AssetAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverItems_HandoverCases_HandoverCaseId",
                        column: x => x.HandoverCaseId,
                        principalTable: "HandoverCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorReviews_ExecutorAccountId",
                table: "ExecutorReviews",
                column: "ExecutorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorReviews_OwnerAccountId",
                table: "ExecutorReviews",
                column: "OwnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutorReviews_WillId",
                table: "ExecutorReviews",
                column: "WillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoverCases_WillId",
                table: "HandoverCases",
                column: "WillId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverItems_AssetAssignmentId",
                table: "HandoverItems",
                column: "AssetAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverItems_HandoverCaseId_AssetAssignmentId",
                table: "HandoverItems",
                columns: new[] { "HandoverCaseId", "AssetAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_ExecutorAccountId",
                table: "LegalCases",
                column: "ExecutorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_Status",
                table: "LegalCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCases_WillId",
                table: "LegalCases",
                column: "WillId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePayments_WillId",
                table: "ServicePayments",
                column: "WillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_AccountId_CreatedAt",
                table: "WalletTransactions",
                columns: new[] { "AccountId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutorReviews");

            migrationBuilder.DropTable(
                name: "HandoverItems");

            migrationBuilder.DropTable(
                name: "LegalCases");

            migrationBuilder.DropTable(
                name: "ServicePayments");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "HandoverCases");

            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "BeneficiaryDesignations");
        }
    }
}
