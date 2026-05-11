using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class RenameAutomatKategorieToKategorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomatyKategorie_AutomatKategorie_KategoriaId",
                table: "AutomatyKategorie");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AutomatKategorie",
                table: "AutomatKategorie");

            migrationBuilder.RenameTable(
                name: "AutomatKategorie",
                newName: "Kategorie");

            migrationBuilder.RenameIndex(
                name: "IX_AutomatKategorie_Nazwa",
                table: "Kategorie",
                newName: "IX_Kategorie_Nazwa");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Kategorie",
                table: "Kategorie",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AutomatyKategorie_Kategorie_KategoriaId",
                table: "AutomatyKategorie",
                column: "KategoriaId",
                principalTable: "Kategorie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomatyKategorie_Kategorie_KategoriaId",
                table: "AutomatyKategorie");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Kategorie",
                table: "Kategorie");

            migrationBuilder.RenameTable(
                name: "Kategorie",
                newName: "AutomatKategorie");

            migrationBuilder.RenameIndex(
                name: "IX_Kategorie_Nazwa",
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
    }
}
