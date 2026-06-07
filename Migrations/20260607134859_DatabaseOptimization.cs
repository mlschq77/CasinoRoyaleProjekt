using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GameName",
                table: "BetRecords",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_BalanceReal",
                table: "Wallets",
                column: "BalanceReal",
                filter: "[BalanceReal] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Nazwa",
                table: "Users",
                column: "Nazwa");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWithdrawals_UserId_CreatedAt",
                table: "StripeWithdrawals",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StripePayments_UserId_CreatedAt",
                table: "StripePayments",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_LoggedAt",
                table: "LoginHistories",
                column: "LoggedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_BalanceReal",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Users_Nazwa",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StripeWithdrawals_UserId_CreatedAt",
                table: "StripeWithdrawals");

            migrationBuilder.DropIndex(
                name: "IX_StripePayments_UserId_CreatedAt",
                table: "StripePayments");

            migrationBuilder.DropIndex(
                name: "IX_LoginHistories_LoggedAt",
                table: "LoginHistories");

            migrationBuilder.AlterColumn<string>(
                name: "GameName",
                table: "BetRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}
