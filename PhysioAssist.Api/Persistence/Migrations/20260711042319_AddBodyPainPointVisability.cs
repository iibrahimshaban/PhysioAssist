using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyPainPointVisability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowPainMap",
                schema: "intake",
                table: "PatientFormSchema",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                schema: "patient",
                table: "Patient",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowPainMap",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "Occupation",
                schema: "patient",
                table: "Patient");
        }
    }
}
