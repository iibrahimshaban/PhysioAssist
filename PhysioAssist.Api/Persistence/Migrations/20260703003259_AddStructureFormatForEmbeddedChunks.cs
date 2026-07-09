using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStructureFormatForEmbeddedChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndOffsetSeconds",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.DropColumn(
                name: "StartOffsetSeconds",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.AddColumn<string>(
                name: "Context",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Focus",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientResponse",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationDetails",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Context",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.DropColumn(
                name: "Focus",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.DropColumn(
                name: "PatientResponse",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.DropColumn(
                name: "RecommendationDetails",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                schema: "session",
                table: "SessionTranscriptionChunk");

            migrationBuilder.AddColumn<int>(
                name: "EndOffsetSeconds",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartOffsetSeconds",
                schema: "session",
                table: "SessionTranscriptionChunk",
                type: "int",
                nullable: true);
        }
    }
}
