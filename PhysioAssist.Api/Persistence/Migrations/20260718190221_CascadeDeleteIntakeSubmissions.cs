using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteIntakeSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.AddForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake",
                column: "FormSchemaId",
                principalSchema: "intake",
                principalTable: "PatientFormSchema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.AddForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake",
                column: "FormSchemaId",
                principalSchema: "intake",
                principalTable: "PatientFormSchema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
