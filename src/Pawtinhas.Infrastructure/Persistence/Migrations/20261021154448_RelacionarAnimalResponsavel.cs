using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pawtinhas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RelacionarAnimalResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsavelId",
                table: "Animais",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Animais_ResponsavelId",
                table: "Animais",
                column: "ResponsavelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Animais_Responsaveis_ResponsavelId",
                table: "Animais",
                column: "ResponsavelId",
                principalTable: "Responsaveis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Animais_Responsaveis_ResponsavelId",
                table: "Animais");

            migrationBuilder.DropIndex(
                name: "IX_Animais_ResponsavelId",
                table: "Animais");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "Animais");
        }
    }
}
