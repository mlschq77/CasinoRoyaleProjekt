using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class RenameAutomatKategorieTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomatyKategorie_KategorieAutomatow_KategoriaId",
                table: "AutomatyKategorie");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KategorieAutomatow",
                table: "KategorieAutomatow");

            migrationBuilder.RenameTable(
                name: "KategorieAutomatow",
                newName: "AutomatKategorie");

            migrationBuilder.RenameIndex(
                name: "IX_KategorieAutomatow_Nazwa",
                table: "AutomatKategorie",
                newName: "IX_AutomatKategorie_Nazwa");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AutomatKategorie",
                table: "AutomatKategorie",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AutomatyKategorie_AutomatKategorie_KategoriaId",
                table: "AutomatyKategorie",
                column: "KategoriaId",
                principalTable: "AutomatKategorie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomatyKategorie_AutomatKategorie_KategoriaId",
                table: "AutomatyKategorie");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AutomatKategorie",
                table: "AutomatKategorie");

            migrationBuilder.RenameTable(
                name: "AutomatKategorie",
                newName: "KategorieAutomatow");

            migrationBuilder.RenameIndex(
                name: "IX_AutomatKategorie_Nazwa",
                table: "KategorieAutomatow",
                newName: "IX_KategorieAutomatow_Nazwa");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KategorieAutomatow",
                table: "KategorieAutomatow",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AutomatyKategorie_KategorieAutomatow_KategoriaId",
                table: "AutomatyKategorie",
                column: "KategoriaId",
                principalTable: "KategorieAutomatow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
