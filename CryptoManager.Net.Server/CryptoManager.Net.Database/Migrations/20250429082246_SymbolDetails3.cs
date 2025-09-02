using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class SymbolDetails3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityStep",
                table: "Symbols",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(16,8)",
                oldPrecision: 16,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceStep",
                table: "Symbols",
                type: "decimal(28,8)",
                precision: 28,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(16,8)",
                oldPrecision: 16,
                oldScale: 8,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityStep",
                table: "Symbols",
                type: "decimal(16,8)",
                precision: 16,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(28,8)",
                oldPrecision: 28,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceStep",
                table: "Symbols",
                type: "decimal(16,8)",
                precision: 16,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(28,8)",
                oldPrecision: 28,
                oldScale: 8,
                oldNullable: true);
        }
    }
}
