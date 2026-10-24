using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegacyVault.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint4AdminStabilization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Faces",
                schema: "public");

            migrationBuilder.DropTable(
                name: "IdpUsers",
                schema: "public");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WalletTransactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "ExecutorProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuspendedAt",
                table: "ExecutorProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuspendedByAccountId",
                table: "ExecutorProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "ExecutorProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WillId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaisedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResolutionAction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDisputes_Wills_WillId",
                        column: x => x.WillId,
                        principalTable: "Wills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtpHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOtps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneOtps_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_AccountId_IdempotencyKey",
                table: "WalletTransactions",
                columns: new[] { "AccountId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorAccountId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "ActorAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDisputes_RaisedByAccountId_CreatedAt",
                table: "PaymentDisputes",
                columns: new[] { "RaisedByAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDisputes_Status",
                table: "PaymentDisputes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDisputes_WillId",
                table: "PaymentDisputes",
                column: "WillId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOtps_AccountId",
                table: "PhoneOtps",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "PaymentDisputes");

            migrationBuilder.DropTable(
                name: "PhoneOtps");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_AccountId_IdempotencyKey",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "SuspendedByAccountId",
                table: "ExecutorProfiles");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "ExecutorProfiles");

            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "IdpUsers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdentityStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdpUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faces",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdpUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Faces_IdpUsers_IdpUserId",
                        column: x => x.IdpUserId,
                        principalSchema: "public",
                        principalTable: "IdpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faces_IdpUserId",
                schema: "public",
                table: "Faces",
                column: "IdpUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdpUsers_Email",
                schema: "public",
                table: "IdpUsers",
                column: "Email",
                unique: true);
        }
    }
}
