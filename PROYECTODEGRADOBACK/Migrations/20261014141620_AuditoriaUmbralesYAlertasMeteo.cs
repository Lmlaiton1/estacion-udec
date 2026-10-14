using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaUmbralesYAlertasMeteo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "Auditoria_Meteo",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccionTomada",
                table: "Alertas_Meteo",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVisto",
                table: "Alertas_Meteo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VistoPorId",
                table: "Alertas_Meteo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_Meteo_VistoPorId",
                table: "Alertas_Meteo",
                column: "VistoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_Meteo_Users_VistoPorId",
                table: "Alertas_Meteo",
                column: "VistoPorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_Meteo_Users_VistoPorId",
                table: "Alertas_Meteo");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_Meteo_VistoPorId",
                table: "Alertas_Meteo");

            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "Auditoria_Meteo");

            migrationBuilder.DropColumn(
                name: "AccionTomada",
                table: "Alertas_Meteo");

            migrationBuilder.DropColumn(
                name: "FechaVisto",
                table: "Alertas_Meteo");

            migrationBuilder.DropColumn(
                name: "VistoPorId",
                table: "Alertas_Meteo");
        }
    }
}
