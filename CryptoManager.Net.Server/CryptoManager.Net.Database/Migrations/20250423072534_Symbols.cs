using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class Symbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseAsset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuoteAsset = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    HighPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    LowPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    ChangePercentage = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Symbols");
        }
    }
}
