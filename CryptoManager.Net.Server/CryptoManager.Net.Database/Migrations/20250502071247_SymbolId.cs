using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class SymbolId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "UserTrade",
                newName: "SymbolId");

            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "UserOrders",
                newName: "SymbolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SymbolId",
                table: "UserTrade",
                newName: "Symbol");

            migrationBuilder.RenameColumn(
                name: "SymbolId",
                table: "UserOrders",
                newName: "Symbol");
        }
    }
}
