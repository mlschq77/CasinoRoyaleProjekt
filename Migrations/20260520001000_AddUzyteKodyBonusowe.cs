using System;
using CasinoRoyale.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Automaty))]
    [Migration("20260520001000_AddUzyteKodyBonusowe")]
    public partial class AddUzyteKodyBonusowe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UzyteKodyBonusowe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    KodBonusowyId = table.Column<int>(type: "int", nullable: false),
                    StripePaymentId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Uzyto = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UzyteKodyBonusowe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UzyteKodyBonusowe_KodyBonusowe_KodBonusowyId",
                        column: x => x.KodBonusowyId,
                        principalTable: "KodyBonusowe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UzyteKodyBonusowe_StripePayments_StripePaymentId",
                        column: x => x.StripePaymentId,
                        principalTable: "StripePayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UzyteKodyBonusowe_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UzyteKodyBonusowe_KodBonusowyId",
                table: "UzyteKodyBonusowe",
                column: "KodBonusowyId");

            migrationBuilder.CreateIndex(
                name: "IX_UzyteKodyBonusowe_StripePaymentId",
                table: "UzyteKodyBonusowe",
                column: "StripePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_UzyteKodyBonusowe_UserId_KodBonusowyId",
                table: "UzyteKodyBonusowe",
                columns: new[] { "UserId", "KodBonusowyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UzyteKodyBonusowe");
        }
    }
}
