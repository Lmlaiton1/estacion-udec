using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class Migration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Device_States",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State_Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Device_Types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Device_Type_Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device_Types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Impacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Impact_Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Measurement_Types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurement_Types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "System_Types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    System_Type_Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_System_Types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Unit_Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Measurement_TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unit_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unit_Measurements_Measurement_Types_Measurement_TypeId",
                        column: x => x.Measurement_TypeId,
                        principalTable: "Measurement_Types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Last_Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Id_Card = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Rols_RolId",
                        column: x => x.RolId,
                        principalTable: "Rols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Device_Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mark = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SN = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    System_TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Device_TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Device_StatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Device_States_Device_StatusId",
                        column: x => x.Device_StatusId,
                        principalTable: "Device_States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Devices_Device_Types_Device_TypeId",
                        column: x => x.Device_TypeId,
                        principalTable: "Device_Types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Devices_System_Types_System_TypeId",
                        column: x => x.System_TypeId,
                        principalTable: "System_Types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audits_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Audits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImpactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    System_TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Events_Impacts_ImpactId",
                        column: x => x.ImpactId,
                        principalTable: "Impacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Events_System_Types_System_TypeId",
                        column: x => x.System_TypeId,
                        principalTable: "System_Types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unit_MeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Measurements_Unit_Measurements_Unit_MeasurementId",
                        column: x => x.Unit_MeasurementId,
                        principalTable: "Unit_Measurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audits_DeviceId",
                table: "Audits",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Audits_UserId",
                table: "Audits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Device_StatusId",
                table: "Devices",
                column: "Device_StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Device_TypeId",
                table: "Devices",
                column: "Device_TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_System_TypeId",
                table: "Devices",
                column: "System_TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_DeviceId",
                table: "Events",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ImpactId",
                table: "Events",
                column: "ImpactId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_System_TypeId",
                table: "Events",
                column: "System_TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_DeviceId",
                table: "Measurements",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_Unit_MeasurementId",
                table: "Measurements",
                column: "Unit_MeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_Unit_Measurements_Measurement_TypeId",
                table: "Unit_Measurements",
                column: "Measurement_TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RolId",
                table: "Users",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Impacts");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Unit_Measurements");

            migrationBuilder.DropTable(
                name: "Rols");

            migrationBuilder.DropTable(
                name: "Device_States");

            migrationBuilder.DropTable(
                name: "Device_Types");

            migrationBuilder.DropTable(
                name: "System_Types");

            migrationBuilder.DropTable(
                name: "Measurement_Types");
        }
    }
}
