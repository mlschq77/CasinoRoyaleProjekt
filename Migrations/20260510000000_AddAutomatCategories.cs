using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomatCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KategorieAutomatow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KategorieAutomatow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomatyKategorie",
                columns: table => new
                {
                    AutomatId = table.Column<int>(type: "int", nullable: false),
                    KategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatyKategorie", x => new { x.AutomatId, x.KategoriaId });
                    table.ForeignKey(
                        name: "FK_AutomatyKategorie_AutomatyInfo_AutomatId",
                        column: x => x.AutomatId,
                        principalTable: "AutomatyInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutomatyKategorie_KategorieAutomatow_KategoriaId",
                        column: x => x.KategoriaId,
                        principalTable: "KategorieAutomatow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO KategorieAutomatow (Nazwa)
                SELECT DISTINCT Kategoria
                FROM AutomatyInfo
                WHERE Kategoria IS NOT NULL AND LTRIM(RTRIM(Kategoria)) <> ''
                """);

            migrationBuilder.Sql("""
                INSERT INTO AutomatyKategorie (AutomatId, KategoriaId)
                SELECT a.Id, k.Id
                FROM AutomatyInfo a
                INNER JOIN KategorieAutomatow k ON k.Nazwa = a.Kategoria
                WHERE a.Kategoria IS NOT NULL AND LTRIM(RTRIM(a.Kategoria)) <> ''
                """);

            migrationBuilder.DropColumn(
                name: "Kategoria",
                table: "AutomatyInfo");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatyKategorie_KategoriaId",
                table: "AutomatyKategorie",
                column: "KategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_KategorieAutomatow_Nazwa",
                table: "KategorieAutomatow",
                column: "Nazwa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kategoria",
                table: "AutomatyInfo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE a
                SET Kategoria = k.Nazwa
                FROM AutomatyInfo a
                INNER JOIN AutomatyKategorie ak ON ak.AutomatId = a.Id
                INNER JOIN KategorieAutomatow k ON k.Id = ak.KategoriaId
                WHERE k.Id = (
                    SELECT TOP 1 ak2.KategoriaId
                    FROM AutomatyKategorie ak2
                    WHERE ak2.AutomatId = a.Id
                    ORDER BY ak2.KategoriaId
                )
                """);

            migrationBuilder.DropTable(
                name: "AutomatyKategorie");

            migrationBuilder.DropTable(
                name: "KategorieAutomatow");
        }
    }
}
