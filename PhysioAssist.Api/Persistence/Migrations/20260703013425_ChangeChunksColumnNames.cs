using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeChunksColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Focus",
                schema: "session",
                table: "SessionTranscriptionChunk",
                newName: "NextSessionFocus");

            migrationBuilder.RenameColumn(
                name: "Context",
                schema: "session",
                table: "SessionTranscriptionChunk",
                newName: "Diagnosis");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.RenameColumn(
                name: "NextSessionFocus",
                schema: "session",
                table: "SessionTranscriptionChunk",
                newName: "Focus");

            migrationBuilder.RenameColumn(
                name: "Diagnosis",
                schema: "session",
                table: "SessionTranscriptionChunk",
                newName: "Context");
        }
    }
}
