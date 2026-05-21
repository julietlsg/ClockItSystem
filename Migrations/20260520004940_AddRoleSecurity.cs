using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClockItSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuardianName",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "GuardianPhone",
                table: "Students",
                newName: "ContactNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactNumber",
                table: "Students",
                newName: "GuardianPhone");

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
