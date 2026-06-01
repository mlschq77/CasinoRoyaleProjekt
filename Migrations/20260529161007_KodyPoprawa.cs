using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class KodyPoprawa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "UzyteKodyBonusowe");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UzyteKodyBonusowe");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "UzyteKodyBonusowe");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UzyteKodyBonusowe");

            migrationBuilder.DropColumn(
                name: "WageringProgress",
                table: "UzyteKodyBonusowe");

            migrationBuilder.DropColumn(
                name: "WageringRequired",
                table: "UzyteKodyBonusowe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "UzyteKodyBonusowe",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UzyteKodyBonusowe",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "UzyteKodyBonusowe",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UzyteKodyBonusowe",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WageringProgress",
                table: "UzyteKodyBonusowe",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WageringRequired",
                table: "UzyteKodyBonusowe",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
