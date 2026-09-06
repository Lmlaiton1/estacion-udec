using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class updateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "ControlRuleActuators",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "ControlRuleActuators");
        }
    }
}
