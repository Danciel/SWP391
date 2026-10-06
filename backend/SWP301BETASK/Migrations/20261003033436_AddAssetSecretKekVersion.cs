using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP301BETASK.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetSecretKekVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KekVersion",
                table: "AssetSecrets",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KekVersion",
                table: "AssetSecrets");
        }
    }
}
