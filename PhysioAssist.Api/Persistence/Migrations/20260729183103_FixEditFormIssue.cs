using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEditFormIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublishedSchemaHash",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedSchemaJson",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedSchemaHash",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "PublishedSchemaJson",
                schema: "intake",
                table: "PatientFormSchema");
        }
    }
}
