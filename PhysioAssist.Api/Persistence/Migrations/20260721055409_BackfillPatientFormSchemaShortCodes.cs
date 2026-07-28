using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPatientFormSchemaShortCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill existing PatientFormSchema rows with unique ShortCodes
            // Use NEWID() to generate random alphanumeric short codes
            migrationBuilder.Sql(@"
                WITH cte AS (
                    SELECT Id,
                           SUBSTRING(CAST(NEWID() AS VARCHAR(36)), 1, 8) AS NewShortCode
                    FROM [intake].[PatientFormSchema]
                    WHERE ShortCode = ''
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
            // No down action - short codes should stay
        }
    }
}
