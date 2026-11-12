using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pawtinhas.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Data de início do alarme: "Semanal" toca no dia da semana dela e "Somente hoje" só nesse dia,
    /// igual no ESP32, no app e no painel. Alarmes que já existiam passam a valer a partir de hoje.
    /// </summary>
    public partial class AlarmeInicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Inicio",
                table: "Alarmes",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql("UPDATE Alarmes SET Inicio = CURDATE();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inicio",
                table: "Alarmes");
        }
    }
}
