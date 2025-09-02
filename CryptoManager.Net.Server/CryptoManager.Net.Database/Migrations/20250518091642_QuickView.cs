using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class QuickView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseAssetType",
                table: "Symbols",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuoteAssetType",
                table: "Symbols",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserQuickViewConfigurations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SymbolId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuickViewConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserQuickViewConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserQuickViewConfigurations_UserId",
                table: "UserQuickViewConfigurations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserQuickViewConfigurations");

            migrationBuilder.DropColumn(
                name: "BaseAssetType",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "QuoteAssetType",
                table: "Symbols");
        }
    }
}
