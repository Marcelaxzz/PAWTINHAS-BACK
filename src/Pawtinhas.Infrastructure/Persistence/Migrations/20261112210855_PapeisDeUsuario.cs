using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pawtinhas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PapeisDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Papel",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ResponsavelId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ResponsavelId",
                table: "Usuarios",
                column: "ResponsavelId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Responsaveis_ResponsavelId",
                table: "Usuarios",
                column: "ResponsavelId",
                principalTable: "Responsaveis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Responsaveis_ResponsavelId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ResponsavelId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Papel",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "Usuarios");
        }
    }
}
