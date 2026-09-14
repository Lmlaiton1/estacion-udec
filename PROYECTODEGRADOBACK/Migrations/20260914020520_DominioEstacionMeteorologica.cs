using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class DominioEstacionMeteorologica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estaciones_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaInstalacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estaciones_Meteo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Imagenes_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Clasificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Confianza = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Procesada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imagenes_Meteo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lecturas_Crudas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Procesado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturas_Crudas", x => x.Id);
                    table.CheckConstraint("CK_Lecturas_Crudas_Payload_ISJSON", "ISJSON(Payload) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Lecturas_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false),
                    SensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    ValorCardinal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PulsosRaw = table.Column<int>(type: "int", nullable: true),
                    IntervaloMs = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturas_Meteo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Predicciones_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaPrediccion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HorizonteHoras = table.Column<int>(type: "int", nullable: false),
                    ValorPredicho = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    NivelConfianza = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ModeloVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predicciones_Meteo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sensores_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Variables = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensores_Meteo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Umbrales_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: true),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorMin = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    ValorMax = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    NivelGravedad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Umbrales_Meteo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Umbrales_Meteo_Users_CreadoPor",
                        column: x => x.CreadoPor,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alertas_Meteo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LecturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UmbralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstacionNumero = table.Column<int>(type: "int", nullable: false),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorRegistrado = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NivelGravedad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Visto = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas_Meteo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertas_Meteo_Lecturas_Meteo_LecturaId",
                        column: x => x.LecturaId,
                        principalTable: "Lecturas_Meteo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alertas_Meteo_Umbrales_Meteo_UmbralId",
                        column: x => x.UmbralId,
                        principalTable: "Umbrales_Meteo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_Meteo_LecturaId",
                table: "Alertas_Meteo",
                column: "LecturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_Meteo_UmbralId",
                table: "Alertas_Meteo",
                column: "UmbralId");

            migrationBuilder.CreateIndex(
                name: "IX_Estaciones_Meteo_Numero",
                table: "Estaciones_Meteo",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecturas_Meteo_EstacionNumero_Variable_FechaHora",
                table: "Lecturas_Meteo",
                columns: new[] { "EstacionNumero", "Variable", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Umbrales_Meteo_CreadoPor",
                table: "Umbrales_Meteo",
                column: "CreadoPor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas_Meteo");

            migrationBuilder.DropTable(
                name: "Estaciones_Meteo");

            migrationBuilder.DropTable(
                name: "Imagenes_Meteo");

            migrationBuilder.DropTable(
                name: "Lecturas_Crudas");

            migrationBuilder.DropTable(
                name: "Predicciones_Meteo");

            migrationBuilder.DropTable(
                name: "Sensores_Meteo");

            migrationBuilder.DropTable(
                name: "Lecturas_Meteo");

            migrationBuilder.DropTable(
                name: "Umbrales_Meteo");
        }
    }
}
