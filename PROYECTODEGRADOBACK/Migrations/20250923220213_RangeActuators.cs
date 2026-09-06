using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class RangeActuators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Max = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Min = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlRules_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlRuleActuators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlRuleActuators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlRuleActuators_ControlRules_ControlRuleId",
                        column: x => x.ControlRuleId,
                        principalTable: "ControlRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ControlRuleActuators_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlRuleActuators_ControlRuleId",
                table: "ControlRuleActuators",
                column: "ControlRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlRuleActuators_DeviceId",
                table: "ControlRuleActuators",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlRules_DeviceId",
                table: "ControlRules",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlRuleActuators");

            migrationBuilder.DropTable(
                name: "ControlRules");
        }
    }
}
