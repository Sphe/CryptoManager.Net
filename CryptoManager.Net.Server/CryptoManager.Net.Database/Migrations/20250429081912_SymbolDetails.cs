using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class SymbolDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinNotionalValue",
                table: "Symbols",
                type: "decimal(16,8)",
                precision: 16,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinTradeQuantity",
                table: "Symbols",
                type: "decimal(16,8)",
                precision: 16,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceDecimals",
                table: "Symbols",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceSignificantFigures",
                table: "Symbols",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceStep",
                table: "Symbols",
                type: "decimal(16,8)",
                precision: 16,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityDecimals",
                table: "Symbols",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityStep",
                table: "Symbols",
                type: "decimal(16,8)",
                precision: 16,
                scale: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinNotionalValue",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "MinTradeQuantity",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "PriceDecimals",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "PriceSignificantFigures",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "PriceStep",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "QuantityDecimals",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "QuantityStep",
                table: "Symbols");
        }
    }
}
