using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class UserTradeOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTrade_UserOrders_OrderId",
                table: "UserTrade");

            migrationBuilder.AlterColumn<string>(
                name: "OrderId",
                table: "UserTrade",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrade_UserOrders_OrderId",
                table: "UserTrade",
                column: "OrderId",
                principalTable: "UserOrders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTrade_UserOrders_OrderId",
                table: "UserTrade");

            migrationBuilder.AlterColumn<string>(
                name: "OrderId",
                table: "UserTrade",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrade_UserOrders_OrderId",
                table: "UserTrade",
                column: "OrderId",
                principalTable: "UserOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
