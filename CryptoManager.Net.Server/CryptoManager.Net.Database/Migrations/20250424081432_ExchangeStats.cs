using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class ExchangeStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuoteVolume",
                table: "AssetStats");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "SymbolStats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "AssetStats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "SymbolStats");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "AssetStats");

            migrationBuilder.AddColumn<decimal>(
                name: "QuoteVolume",
                table: "AssetStats",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: true);
        }
    }
}
