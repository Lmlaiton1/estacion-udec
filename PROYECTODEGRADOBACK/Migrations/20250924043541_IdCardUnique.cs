using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RR_Nueva_Naturaleza.Migrations
{
    /// <inheritdoc />
    public partial class IdCardUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Id_Card",
                table: "Users",
                column: "Id_Card",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Id_Card",
                table: "Users");
        }
    }
}
