using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pawtinhas.Infrastructure.Persistence.Migrations
{
    /// <summary>Foto do pacote da ração, mostrada ao lado do cartão no catálogo.</summary>
    public partial class RacaoFoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Racoes",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Racoes");
        }
    }
}
