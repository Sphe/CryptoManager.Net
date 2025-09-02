using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoManager.Net.Database.Migrations
{
    /// <inheritdoc />
    public partial class UserTradeOrder2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTrade_UserOrders_OrderId",
                table: "UserTrade");

            migrationBuilder.DropIndex(
                name: "IX_UserTrade_OrderId",
                table: "UserTrade");

            migrationBuilder.AlterColumn<string>(
                name: "OrderId",
                table: "UserTrade",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserTrade",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserTrade_UserId",
                table: "UserTrade",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrade_Users_UserId",
                table: "UserTrade",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTrade_Users_UserId",
                table: "UserTrade");

            migrationBuilder.DropIndex(
                name: "IX_UserTrade_UserId",
                table: "UserTrade");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserTrade");

            migrationBuilder.AlterColumn<string>(
                name: "OrderId",
                table: "UserTrade",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTrade_OrderId",
                table: "UserTrade",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrade_UserOrders_OrderId",
                table: "UserTrade",
                column: "OrderId",
                principalTable: "UserOrders",
                principalColumn: "Id");
        }
    }
}
