using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class CultivoAuditoriaYEliminacionLogica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sensores_Cultivo_CultivoId_Clave",
                table: "Sensores_Cultivo");

            migrationBuilder.DropIndex(
                name: "IX_Cultivos_Numero",
                table: "Cultivos");

            migrationBuilder.DropIndex(
                name: "IX_Actuadores_Cultivo_CultivoId_Clave",
                table: "Actuadores_Cultivo");

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Umbrales_Cultivo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Sensores_Cultivo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Etapas_Cultivo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Cultivos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Alarmas_Cultivo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Actuadores_Cultivo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Auditoria_Cultivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CultivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoEntidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreEntidad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria_Cultivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auditoria_Cultivo_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_Cultivo_CultivoId_Clave",
                table: "Sensores_Cultivo",
                columns: new[] { "CultivoId", "Clave" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cultivos_Numero",
                table: "Cultivos",
                column: "Numero",
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Actuadores_Cultivo_CultivoId_Clave",
                table: "Actuadores_Cultivo",
                columns: new[] { "CultivoId", "Clave" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Cultivo_CultivoId_Fecha",
                table: "Auditoria_Cultivo",
                columns: new[] { "CultivoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Cultivo_UsuarioId",
                table: "Auditoria_Cultivo",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditoria_Cultivo");

            migrationBuilder.DropIndex(
                name: "IX_Sensores_Cultivo_CultivoId_Clave",
                table: "Sensores_Cultivo");

            migrationBuilder.DropIndex(
                name: "IX_Cultivos_Numero",
                table: "Cultivos");

            migrationBuilder.DropIndex(
                name: "IX_Actuadores_Cultivo_CultivoId_Clave",
                table: "Actuadores_Cultivo");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Umbrales_Cultivo");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Sensores_Cultivo");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Etapas_Cultivo");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Cultivos");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Alarmas_Cultivo");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Actuadores_Cultivo");

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_Cultivo_CultivoId_Clave",
                table: "Sensores_Cultivo",
                columns: new[] { "CultivoId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cultivos_Numero",
                table: "Cultivos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actuadores_Cultivo_CultivoId_Clave",
                table: "Actuadores_Cultivo",
                columns: new[] { "CultivoId", "Clave" },
                unique: true);
        }
    }
}
