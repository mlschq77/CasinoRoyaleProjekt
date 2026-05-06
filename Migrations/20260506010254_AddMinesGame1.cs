using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class AddMinesGame1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MinesGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GridSize = table.Column<int>(type: "int", nullable: false),
                    MineCount = table.Column<int>(type: "int", nullable: false),
                    MinePositions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevealedPositions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinesGames", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinesGames");
        }
    }
}
