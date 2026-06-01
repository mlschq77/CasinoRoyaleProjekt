using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class KodyDoWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveBonusId",
                table: "Wallets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BonusExpiresAt",
                table: "Wallets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WageringProgress",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WageringRequired",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveBonusId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "BonusExpiresAt",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "WageringProgress",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "WageringRequired",
                table: "Wallets");
        }
    }
}
