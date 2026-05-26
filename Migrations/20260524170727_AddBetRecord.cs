using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class AddBetRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BetRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountFromBonus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SessionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Settled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BetRecords_UserId_SessionKey_Settled",
                table: "BetRecords",
                columns: new[] { "UserId", "SessionKey", "Settled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BetRecords");
        }
    }
}
