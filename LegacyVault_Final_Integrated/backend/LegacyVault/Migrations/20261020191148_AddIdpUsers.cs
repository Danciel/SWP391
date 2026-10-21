using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegacyVault.Migrations
{
    /// <inheritdoc />
    public partial class AddIdpUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");


            migrationBuilder.CreateTable(
                name: "IdpUsers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IdentityStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    EmbeddingJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "AssetSecrets");

            migrationBuilder.DropTable(
                name: "AssetVerifications");

            migrationBuilder.DropTable(
                name: "DmsCheckIns");

            migrationBuilder.DropTable(
                name: "EmailOtps");

            migrationBuilder.DropTable(
                name: "ExecutorAssignments");

            migrationBuilder.DropTable(
                name: "ExecutorProfiles");

            migrationBuilder.DropTable(
                name: "Faces",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MockExternalAccounts");

            migrationBuilder.DropTable(
                name: "BeneficiaryDesignations");

            migrationBuilder.DropTable(
                name: "WillAssets");

            migrationBuilder.DropTable(
                name: "DmsConfigurations");

            migrationBuilder.DropTable(
                name: "IdpUsers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Wills");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
