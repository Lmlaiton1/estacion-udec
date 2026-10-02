using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyLecturaMeteoCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Estaciones_Meteo_Numero",
                table: "Estaciones_Meteo");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Estaciones_Meteo_Numero",
                table: "Estaciones_Meteo",
                column: "Numero");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturas_Meteo_SensorId",
                table: "Lecturas_Meteo",
                column: "SensorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturas_Meteo_Estaciones_Meteo_EstacionNumero",
                table: "Lecturas_Meteo",
                column: "EstacionNumero",
                principalTable: "Estaciones_Meteo",
                principalColumn: "Numero",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturas_Meteo_Sensores_Meteo_SensorId",
                table: "Lecturas_Meteo",
                column: "SensorId",
                principalTable: "Sensores_Meteo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lecturas_Meteo_Estaciones_Meteo_EstacionNumero",
                table: "Lecturas_Meteo");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturas_Meteo_Sensores_Meteo_SensorId",
                table: "Lecturas_Meteo");

            migrationBuilder.DropIndex(
                name: "IX_Lecturas_Meteo_SensorId",
                table: "Lecturas_Meteo");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Estaciones_Meteo_Numero",
                table: "Estaciones_Meteo");

            migrationBuilder.CreateIndex(
                name: "IX_Estaciones_Meteo_Numero",
                table: "Estaciones_Meteo",
                column: "Numero",
                unique: true);
        }
    }
}
