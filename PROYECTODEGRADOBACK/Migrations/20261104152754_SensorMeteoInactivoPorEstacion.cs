using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class SensorMeteoInactivoPorEstacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sensores_Meteo_Inactivos",
                columns: table => new
                {
                    SensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensores_Meteo_Inactivos", x => new { x.SensorId, x.EstacionNumero });
                    table.ForeignKey(
                        name: "FK_Sensores_Meteo_Inactivos_Estaciones_Meteo_EstacionNumero",
                        column: x => x.EstacionNumero,
                        principalTable: "Estaciones_Meteo",
                        principalColumn: "Numero",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sensores_Meteo_Inactivos_Sensores_Meteo_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensores_Meteo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_Meteo_Inactivos_EstacionNumero",
                table: "Sensores_Meteo_Inactivos",
                column: "EstacionNumero");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sensores_Meteo_Inactivos");
        }
    }
}
