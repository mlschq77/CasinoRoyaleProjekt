using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class MakeWageringMultiplierRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "WageringMultiplier",
                table: "KodyBonusowe",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "WageringMultiplier",
                table: "KodyBonusowe",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
