using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class IsUniqueChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Unit_Measurements_Name",
                table: "Unit_Measurements",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_System_Types_System_Type_Name",
                table: "System_Types",
                column: "System_Type_Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Measurement_Types_Name",
                table: "Measurement_Types",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Impacts_Impact_Type",
                table: "Impacts",
                column: "Impact_Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Device_Name",
                table: "Devices",
                column: "Device_Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Device_Types_Device_Type_Name",
                table: "Device_Types",
                column: "Device_Type_Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Device_States_State_Name",
                table: "Device_States",
                column: "State_Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Unit_Measurements_Name",
                table: "Unit_Measurements");

            migrationBuilder.DropIndex(
                name: "IX_System_Types_System_Type_Name",
                table: "System_Types");

            migrationBuilder.DropIndex(
                name: "IX_Measurement_Types_Name",
                table: "Measurement_Types");

            migrationBuilder.DropIndex(
                name: "IX_Impacts_Impact_Type",
                table: "Impacts");

            migrationBuilder.DropIndex(
                name: "IX_Devices_Device_Name",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Device_Types_Device_Type_Name",
                table: "Device_Types");

            migrationBuilder.DropIndex(
                name: "IX_Device_States_State_Name",
                table: "Device_States");
        }
    }
}
