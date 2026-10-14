using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class CultivoDispositivosNuevaNaturaleza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Sensores_Cultivo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "Actuadores_Cultivo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sensores_Cultivo_DeviceId",
                table: "Sensores_Cultivo",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Actuadores_Cultivo_DeviceId",
                table: "Actuadores_Cultivo",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actuadores_Cultivo_Devices_DeviceId",
                table: "Actuadores_Cultivo",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sensores_Cultivo_Devices_DeviceId",
                table: "Sensores_Cultivo",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actuadores_Cultivo_Devices_DeviceId",
                table: "Actuadores_Cultivo");

            migrationBuilder.DropForeignKey(
                name: "FK_Sensores_Cultivo_Devices_DeviceId",
                table: "Sensores_Cultivo");

            migrationBuilder.DropIndex(
                name: "IX_Sensores_Cultivo_DeviceId",
                table: "Sensores_Cultivo");

            migrationBuilder.DropIndex(
                name: "IX_Actuadores_Cultivo_DeviceId",
                table: "Actuadores_Cultivo");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Sensores_Cultivo");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Actuadores_Cultivo");
        }
    }
}
