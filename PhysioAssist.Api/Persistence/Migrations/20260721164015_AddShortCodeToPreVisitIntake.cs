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

            // Backfill: every existing row got '' from the default above, which
            // can't satisfy the unique index below. Give each pre-existing row
            // a distinct 8-char code before the index gets created.
            migrationBuilder.Sql(@"
        ;WITH numbered AS (
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
            FROM intake.PreVisitIntake
            WHERE ShortCode = '' OR ShortCode IS NULL
        )
        UPDATE p
        SET p.ShortCode = 'L' + RIGHT('0000000' + CAST(n.rn AS VARCHAR(7)), 7)
        FROM intake.PreVisitIntake p
        JOIN numbered n ON p.Id = n.Id;
    ");

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
