using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class Stats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "QuoteAsset",
                table: "Symbols",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Symbols",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymbolStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaseAsset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuoteAsset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Symbols_QuoteAsset",
                table: "Symbols",
                column: "QuoteAsset");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetStats");

            migrationBuilder.DropTable(
                name: "SymbolStats");

            migrationBuilder.DropIndex(
                name: "IX_Symbols_QuoteAsset",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Symbols");

            migrationBuilder.AlterColumn<string>(
                name: "QuoteAsset",
                table: "Symbols",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
