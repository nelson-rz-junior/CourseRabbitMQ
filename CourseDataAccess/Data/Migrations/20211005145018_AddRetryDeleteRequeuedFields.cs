using Microsoft.EntityFrameworkCore.Migrations;

namespace CourseDataAccess.Data.Migrations
{
    public partial class AddRetryDeleteRequeuedFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Requeued",
                table: "Courses",
                newName: "Error");

            migrationBuilder.AddColumn<int>(
                name: "Retry",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Retry",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "Error",
                table: "Courses",
                newName: "Requeued");
        }
    }
}
