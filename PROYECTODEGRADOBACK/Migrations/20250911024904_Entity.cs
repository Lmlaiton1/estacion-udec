using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class Entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Measurements_Unit_Measurements_Unit_MeasurementId",
                table: "Measurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Unit_Measurements_Measurement_Types_Measurement_TypeId",
                table: "Unit_Measurements");

            migrationBuilder.DropIndex(
                name: "IX_Unit_Measurements_Measurement_TypeId",
                table: "Unit_Measurements");

            migrationBuilder.DropColumn(
                name: "Measurement_TypeId",
                table: "Unit_Measurements");

            migrationBuilder.RenameColumn(
                name: "Unit_MeasurementId",
                table: "Measurements",
                newName: "Measurement_TypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Measurements_Unit_MeasurementId",
                table: "Measurements",
                newName: "IX_Measurements_Measurement_TypeId");

            migrationBuilder.AddColumn<Guid>(
                name: "Unit_MeasurementId",
                table: "Measurement_Types",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Measurement_Types_Unit_MeasurementId",
                table: "Measurement_Types",
                column: "Unit_MeasurementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Measurement_Types_Unit_Measurements_Unit_MeasurementId",
                table: "Measurement_Types",
                column: "Unit_MeasurementId",
                principalTable: "Unit_Measurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Measurements_Measurement_Types_Measurement_TypeId",
                table: "Measurements",
                column: "Measurement_TypeId",
                principalTable: "Measurement_Types",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Measurement_Types_Unit_Measurements_Unit_MeasurementId",
                table: "Measurement_Types");

            migrationBuilder.DropForeignKey(
                name: "FK_Measurements_Measurement_Types_Measurement_TypeId",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_Measurement_Types_Unit_MeasurementId",
                table: "Measurement_Types");

            migrationBuilder.DropColumn(
                name: "Unit_MeasurementId",
                table: "Measurement_Types");

            migrationBuilder.RenameColumn(
                name: "Measurement_TypeId",
                table: "Measurements",
                newName: "Unit_MeasurementId");

            migrationBuilder.RenameIndex(
                name: "IX_Measurements_Measurement_TypeId",
                table: "Measurements",
                newName: "IX_Measurements_Unit_MeasurementId");

            migrationBuilder.AddColumn<Guid>(
                name: "Measurement_TypeId",
                table: "Unit_Measurements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Unit_Measurements_Measurement_TypeId",
                table: "Unit_Measurements",
                column: "Measurement_TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Measurements_Unit_Measurements_Unit_MeasurementId",
                table: "Measurements",
                column: "Unit_MeasurementId",
                principalTable: "Unit_Measurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Unit_Measurements_Measurement_Types_Measurement_TypeId",
                table: "Unit_Measurements",
                column: "Measurement_TypeId",
                principalTable: "Measurement_Types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
