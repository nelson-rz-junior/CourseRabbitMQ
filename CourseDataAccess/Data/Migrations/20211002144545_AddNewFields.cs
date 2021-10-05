using Microsoft.EntityFrameworkCore.Migrations;

namespace CourseDataAccess.Data.Migrations
{
    public partial class AddNewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Queued",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Requeued",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Queued",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Requeued",
                table: "Courses");
        }
    }
}
