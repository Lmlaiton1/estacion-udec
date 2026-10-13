using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class AddControlCultivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cultivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Especie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Variedad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ubicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AreaM2 = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    FechaSiembra = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstacionNumero = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cultivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cultivos_Users_CreadoPor",
                        column: x => x.CreadoPor,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Eventos_Dispositivo_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDispositivo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DispositivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreDispositivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlarmaCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UmbralCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos_Dispositivo_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eventos_Dispositivo_Cultivo_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Actuadores_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Potencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Especificaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Modo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Encendido = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaUltimoCambio = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actuadores_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actuadores_Cultivo_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalTable: "Cultivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Etapas_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    DuracionDias = table.Column<int>(type: "int", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapas_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etapas_Cultivo_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalTable: "Cultivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sensores_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RangoMin = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    RangoMax = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Precision = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Especificaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaInstalacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensores_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sensores_Cultivo_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalTable: "Cultivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lecturas_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensorCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturas_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lecturas_Cultivo_Sensores_Cultivo_SensorCultivoId",
                        column: x => x.SensorCultivoId,
                        principalTable: "Sensores_Cultivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Umbrales_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensorCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EtapaCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValorMin = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    ValorMax = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    NivelGravedad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActuadorCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EncenderActuador = table.Column<bool>(type: "bit", nullable: true),
                    RevertirAlNormalizar = table.Column<bool>(type: "bit", nullable: false),
                    MinutosEscalamientoSms = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Umbrales_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Umbrales_Cultivo_Actuadores_Cultivo_ActuadorCultivoId",
                        column: x => x.ActuadorCultivoId,
                        principalTable: "Actuadores_Cultivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Umbrales_Cultivo_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalTable: "Cultivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Umbrales_Cultivo_Etapas_Cultivo_EtapaCultivoId",
                        column: x => x.EtapaCultivoId,
                        principalTable: "Etapas_Cultivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Umbrales_Cultivo_Sensores_Cultivo_SensorCultivoId",
                        column: x => x.SensorCultivoId,
                        principalTable: "Sensores_Cultivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Umbrales_Cultivo_Users_CreadoPor",
                        column: x => x.CreadoPor,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alarmas_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UmbralCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensorCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EtapaNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Variable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorRegistrado = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    UltimoValor = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Ocurrencias = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NivelGravedad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaDisparo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimaOcurrencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VistaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaVista = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AtendidaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaAtencion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResueltaPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Solucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SmsEnviado = table.Column<bool>(type: "bit", nullable: false),
                    FechaSms = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarmas_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarmas_Cultivo_Cultivos_CultivoId",
                        column: x => x.CultivoId,
                        principalTable: "Cultivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alarmas_Cultivo_Umbrales_Cultivo_UmbralCultivoId",
                        column: x => x.UmbralCultivoId,
                        principalTable: "Umbrales_Cultivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alarmas_Cultivo_Users_AtendidaPorId",
                        column: x => x.AtendidaPorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alarmas_Cultivo_Users_ResueltaPorId",
                        column: x => x.ResueltaPorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alarmas_Cultivo_Users_VistaPorId",
                        column: x => x.VistaPorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bitacora_Alarmas_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlarmaCultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacora_Alarmas_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bitacora_Alarmas_Cultivo_Alarmas_Cultivo_AlarmaCultivoId",
                        column: x => x.AlarmaCultivoId,
                        principalTable: "Alarmas_Cultivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bitacora_Alarmas_Cultivo_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actuadores_Cultivo_CultivoId_Clave",
                table: "Actuadores_Cultivo",
                columns: new[] { "CultivoId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alarmas_Cultivo_AtendidaPorId",
                table: "Alarmas_Cultivo",
                column: "AtendidaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarmas_Cultivo_CultivoId_Estado",
                table: "Alarmas_Cultivo",
                columns: new[] { "CultivoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Alarmas_Cultivo_ResueltaPorId",
                table: "Alarmas_Cultivo",
                column: "ResueltaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarmas_Cultivo_UmbralCultivoId",
                table: "Alarmas_Cultivo",
                column: "UmbralCultivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarmas_Cultivo_VistaPorId",
                table: "Alarmas_Cultivo",
                column: "VistaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_Alarmas_Cultivo_AlarmaCultivoId",
                table: "Bitacora_Alarmas_Cultivo",
                column: "AlarmaCultivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_Alarmas_Cultivo_UsuarioId",
                table: "Bitacora_Alarmas_Cultivo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cultivos_CreadoPor",
                table: "Cultivos",
                column: "CreadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Cultivos_Numero",
                table: "Cultivos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etapas_Cultivo_CultivoId",
                table: "Etapas_Cultivo",
                column: "CultivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Dispositivo_Cultivo_CultivoId_Fecha",
                table: "Eventos_Dispositivo_Cultivo",
                columns: new[] { "CultivoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Dispositivo_Cultivo_UsuarioId",
                table: "Eventos_Dispositivo_Cultivo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturas_Cultivo_SensorCultivoId_FechaHora",
                table: "Lecturas_Cultivo",
                columns: new[] { "SensorCultivoId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_Cultivo_CultivoId_Clave",
                table: "Sensores_Cultivo",
                columns: new[] { "CultivoId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Umbrales_Cultivo_ActuadorCultivoId",
                table: "Umbrales_Cultivo",
                column: "ActuadorCultivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Umbrales_Cultivo_CreadoPor",
                table: "Umbrales_Cultivo",
                column: "CreadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Umbrales_Cultivo_CultivoId",
                table: "Umbrales_Cultivo",
                column: "CultivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Umbrales_Cultivo_EtapaCultivoId",
                table: "Umbrales_Cultivo",
                column: "EtapaCultivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Umbrales_Cultivo_SensorCultivoId",
                table: "Umbrales_Cultivo",
                column: "SensorCultivoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bitacora_Alarmas_Cultivo");

            migrationBuilder.DropTable(
                name: "Eventos_Dispositivo_Cultivo");

            migrationBuilder.DropTable(
                name: "Lecturas_Cultivo");

            migrationBuilder.DropTable(
                name: "Alarmas_Cultivo");

            migrationBuilder.DropTable(
                name: "Umbrales_Cultivo");

            migrationBuilder.DropTable(
                name: "Actuadores_Cultivo");

            migrationBuilder.DropTable(
                name: "Etapas_Cultivo");

            migrationBuilder.DropTable(
                name: "Sensores_Cultivo");

            migrationBuilder.DropTable(
                name: "Cultivos");
        }
    }
}
