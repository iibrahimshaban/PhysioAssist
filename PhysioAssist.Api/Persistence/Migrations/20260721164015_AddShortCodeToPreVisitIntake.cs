using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShortCodeToPreVisitIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PreVisitIntake_ShortCode",
                schema: "intake",
                table: "PreVisitIntake",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PreVisitIntake_ShortCode",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                schema: "intake",
                table: "PreVisitIntake");
        }
    }
}
