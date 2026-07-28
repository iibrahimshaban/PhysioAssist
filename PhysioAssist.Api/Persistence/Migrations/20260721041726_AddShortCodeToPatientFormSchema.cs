using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShortCodeToPatientFormSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            // Backfill unique short codes for existing rows before adding the unique index.
            migrationBuilder.Sql(@"
                WITH NumberedForms AS (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAt) AS RowNum
                    FROM [intake].[PatientFormSchema]
                )
                UPDATE f
                SET f.ShortCode = CONCAT('FORM', RIGHT('000000' + CAST(nf.RowNum AS NVARCHAR(6)), 6))
                FROM [intake].[PatientFormSchema] f
                INNER JOIN NumberedForms nf ON f.Id = nf.Id;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFormSchema_ShortCode",
                schema: "intake",
                table: "PatientFormSchema",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientFormSchema_ShortCode",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                schema: "intake",
                table: "PatientFormSchema");
        }
    }
}
