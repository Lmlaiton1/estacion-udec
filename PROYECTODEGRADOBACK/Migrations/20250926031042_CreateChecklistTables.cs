using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class CreateChecklistTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistHeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistHeaders_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispositivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MedicionSensor = table.Column<double>(type: "float", nullable: true),
                    MedicionManual = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistDetails_ChecklistHeaders_ChecklistHeaderId",
                        column: x => x.ChecklistHeaderId,
                        principalTable: "ChecklistHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChecklistDetails_Devices_DispositivoId",
                        column: x => x.DispositivoId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDetails_ChecklistHeaderId",
                table: "ChecklistDetails",
                column: "ChecklistHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDetails_DispositivoId",
                table: "ChecklistDetails",
                column: "DispositivoId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistHeaders_UsuarioId",
                table: "ChecklistHeaders",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistDetails");

            migrationBuilder.DropTable(
                name: "ChecklistHeaders");
        }
    }
}
