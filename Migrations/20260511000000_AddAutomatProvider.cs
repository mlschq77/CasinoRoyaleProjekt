using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomatProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomatProviderzy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nazwa = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomatProviderzy", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "AutomatyInfo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomatyInfo_ProviderId",
                table: "AutomatyInfo",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomatProviderzy_Nazwa",
                table: "AutomatProviderzy",
                column: "Nazwa",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AutomatyInfo_AutomatProviderzy_ProviderId",
                table: "AutomatyInfo",
                column: "ProviderId",
                principalTable: "AutomatProviderzy",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomatyInfo_AutomatProviderzy_ProviderId",
                table: "AutomatyInfo");

            migrationBuilder.DropIndex(
                name: "IX_AutomatyInfo_ProviderId",
                table: "AutomatyInfo");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "AutomatyInfo");

            migrationBuilder.DropTable(
                name: "AutomatProviderzy");
        }
    }
}
