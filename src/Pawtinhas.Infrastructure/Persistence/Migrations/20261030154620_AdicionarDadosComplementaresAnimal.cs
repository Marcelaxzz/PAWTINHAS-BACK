using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pawtinhas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDadosComplementaresAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Animais",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Localizacao",
                table: "Animais",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Personalidade",
                table: "Animais",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Animais");

            migrationBuilder.DropColumn(
                name: "Localizacao",
                table: "Animais");

            migrationBuilder.DropColumn(
                name: "Personalidade",
                table: "Animais");
        }
    }
}
