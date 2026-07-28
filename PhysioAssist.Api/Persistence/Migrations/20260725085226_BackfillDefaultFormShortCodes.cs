using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDefaultFormShortCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropIndex(
                name: "IX_PatientFormSchema_DoctorId_OriginalFormId",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.AddForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake",
                column: "FormSchemaId",
                principalSchema: "intake",
                principalTable: "PatientFormSchema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill any PatientFormSchema rows still missing a ShortCode
            migrationBuilder.Sql(@"
                WITH cte AS (
                    SELECT Id,
                           SUBSTRING(CAST(NEWID() AS VARCHAR(36)), 1, 8) AS NewShortCode
                    FROM [intake].[PatientFormSchema]
                    WHERE ShortCode = '' OR ShortCode IS NULL
                )
                UPDATE f
                SET f.ShortCode = cte.NewShortCode
                FROM [intake].[PatientFormSchema] f
                JOIN cte ON f.Id = cte.Id
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFormSchema_DoctorId_OriginalFormId",
                schema: "intake",
                table: "PatientFormSchema",
                columns: new[] { "DoctorId", "OriginalFormId" });

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
    }
}
