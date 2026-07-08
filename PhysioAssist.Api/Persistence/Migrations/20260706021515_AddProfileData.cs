using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "About",
                schema: "auth",
                table: "Doctor",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicAddress",
                schema: "auth",
                table: "Doctor",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "auth",
                table: "Doctor",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                schema: "auth",
                table: "Doctor",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "About",
                schema: "auth",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "ClinicAddress",
                schema: "auth",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "auth",
                table: "Doctor");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                schema: "auth",
                table: "Doctor");
        }
    }
}
