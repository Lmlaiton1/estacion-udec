using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class CultivoVariasEstaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cultivo_Estaciones",
                columns: table => new
                {
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cultivo_Estaciones", x => new { x.CultivoId, x.EstacionNumero });
                    table.ForeignKey(
                        name: "FK_Cultivo_Estaciones_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalTable: "Cultivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cultivo_Estaciones_Estaciones_Meteo_EstacionNumero",
                        column: x => x.EstacionNumero,
                        principalTable: "Estaciones_Meteo",
                        principalColumn: "Numero",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cultivo_Estaciones_EstacionNumero",
                table: "Cultivo_Estaciones",
                column: "EstacionNumero");

            // Conserva la estación que ya tenía cada cultivo antes de quitar la columna
            migrationBuilder.Sql(@"INSERT INTO Cultivo_Estaciones (CultivoId, EstacionNumero)
                SELECT c.Id, c.EstacionNumero FROM Cultivos c
                WHERE c.EstacionNumero IS NOT NULL
                  AND EXISTS (SELECT 1 FROM Estaciones_Meteo e WHERE e.Numero = c.EstacionNumero)");

            migrationBuilder.DropColumn(
                name: "EstacionNumero",
                table: "Cultivos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstacionNumero",
                table: "Cultivos",
                type: "int",
                nullable: true);

            // Al revertir solo cabe una estación: se conserva la de menor número
            migrationBuilder.Sql(@"UPDATE c SET EstacionNumero =
                (SELECT MIN(ce.EstacionNumero) FROM Cultivo_Estaciones ce WHERE ce.CultivoId = c.Id)
                FROM Cultivos c");

            migrationBuilder.DropTable(
                name: "Cultivo_Estaciones");
        }
    }
}
