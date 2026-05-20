using System;
using CasinoRoyale.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Automaty))]
    [Migration("20260511004000_AddKodyBonusowe")]
    public partial class AddKodyBonusowe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KodyBonusowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MinimalnaWplata = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusProcentowy = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BonusKwotowy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Utworzono = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaznyDo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KodyBonusowe", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KodyBonusowe_Kod",
                table: "KodyBonusowe",
                column: "Kod",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KodyBonusowe");
        }
    }
}
