using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// SUPERSEDED: this migration briefly set FK_PreVisitIntake_PatientFormSchema_FormSchemaId
    /// to Cascade, but a later migration (20260725085226_BackfillDefaultFormShortCodes)
    /// reverted it to Restrict. The schema-delete 500 is instead prevented in code by
    /// IntakeService.DeleteFormSchemaAsync's submission-count guard. Do not re-apply Cascade here.
    /// </remarks>
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
