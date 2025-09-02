using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class AssetTypeIndex2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssetStats_AssetType",
                table: "AssetStats",
                column: "AssetType")
                .Annotation("SqlServer:Include", new[] { "Value", "Volume", "ChangePercentage", "Asset" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetStats_AssetType",
                table: "AssetStats");
        }
    }
}
