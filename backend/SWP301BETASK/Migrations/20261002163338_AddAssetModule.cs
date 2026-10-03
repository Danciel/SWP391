using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP301BETASK.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VerificationStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetSecrets",
                columns: table => new
                {
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedPayload = table.Column<byte[]>(type: "bytea", nullable: false),
                    DataNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    DataTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptedDek = table.Column<byte[]>(type: "bytea", nullable: true),
                    DekNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    DekTag = table.Column<byte[]>(type: "bytea", nullable: true),
                    EncryptionAlgorithm = table.Column<string>(type: "text", nullable: false),
                    EncryptionVersion = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetSecrets", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_AssetSecrets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ExternalIdentifier = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetVerifications_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_OwnerAccountId",
                table: "Assets",
                column: "OwnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetVerifications_AssetId",
                table: "AssetVerifications",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetSecrets");

            migrationBuilder.DropTable(
                name: "AssetVerifications");

            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
