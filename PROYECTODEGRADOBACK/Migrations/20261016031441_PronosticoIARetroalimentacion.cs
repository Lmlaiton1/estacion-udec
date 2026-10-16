using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class PronosticoIARetroalimentacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LimiteInferior",
                table: "Predicciones_Meteo",
                type: "decimal(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LimiteSuperior",
                table: "Predicciones_Meteo",
                type: "decimal(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PronosticoId",
                table: "Predicciones_Meteo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pronosticos_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneradoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaPronosticado = table.Column<DateTime>(type: "date", nullable: false),
                    OrigenDatos = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModeloVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UmbralLluvia = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    FactorSol = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    Advertencia = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VentanasEntrenamiento = table.Column<int>(type: "int", nullable: false),
                    SegundosEjecucion = table.Column<decimal>(type: "decimal(8,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pronosticos_Meteo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pronosticos_Meteo_Users_GeneradoPorId",
                        column: x => x.GeneradoPorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pronosticos_Franja_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PronosticoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Franja = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    CondicionPrevista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemperaturaMedia = table.Column<decimal>(type: "decimal(8,3)", nullable: false),
                    TemperaturaMin = table.Column<decimal>(type: "decimal(8,3)", nullable: false),
                    TemperaturaMax = table.Column<decimal>(type: "decimal(8,3)", nullable: false),
                    HumedadMedia = table.Column<decimal>(type: "decimal(8,3)", nullable: false),
                    LuzMediaLux = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LluviaMm = table.Column<decimal>(type: "decimal(8,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pronosticos_Franja_Meteo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pronosticos_Franja_Meteo_Pronosticos_Meteo_PronosticoId",
                        column: x => x.PronosticoId,
                        principalTable: "Pronosticos_Meteo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Retroalimentacion_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FranjaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CondicionReal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Acerto = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retroalimentacion_Meteo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Retroalimentacion_Meteo_Pronosticos_Franja_Meteo_FranjaId",
                        column: x => x.FranjaId,
                        principalTable: "Pronosticos_Franja_Meteo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Retroalimentacion_Meteo_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predicciones_Meteo_PronosticoId",
                table: "Predicciones_Meteo",
                column: "PronosticoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pronosticos_Franja_Meteo_PronosticoId",
                table: "Pronosticos_Franja_Meteo",
                column: "PronosticoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pronosticos_Meteo_DiaPronosticado",
                table: "Pronosticos_Meteo",
                column: "DiaPronosticado");

            migrationBuilder.CreateIndex(
                name: "IX_Pronosticos_Meteo_GeneradoPorId",
                table: "Pronosticos_Meteo",
                column: "GeneradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Retroalimentacion_Meteo_FranjaId",
                table: "Retroalimentacion_Meteo",
                column: "FranjaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Retroalimentacion_Meteo_UsuarioId",
                table: "Retroalimentacion_Meteo",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Predicciones_Meteo_Pronosticos_Meteo_PronosticoId",
                table: "Predicciones_Meteo",
                column: "PronosticoId",
                principalTable: "Pronosticos_Meteo",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predicciones_Meteo_Pronosticos_Meteo_PronosticoId",
                table: "Predicciones_Meteo");

            migrationBuilder.DropTable(
                name: "Retroalimentacion_Meteo");

            migrationBuilder.DropTable(
                name: "Pronosticos_Franja_Meteo");

            migrationBuilder.DropTable(
                name: "Pronosticos_Meteo");

            migrationBuilder.DropIndex(
                name: "IX_Predicciones_Meteo_PronosticoId",
                table: "Predicciones_Meteo");

            migrationBuilder.DropColumn(
                name: "LimiteInferior",
                table: "Predicciones_Meteo");

            migrationBuilder.DropColumn(
                name: "LimiteSuperior",
                table: "Predicciones_Meteo");

            migrationBuilder.DropColumn(
                name: "PronosticoId",
                table: "Predicciones_Meteo");
        }
    }
}
