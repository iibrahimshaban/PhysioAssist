using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateRealtionBetweenSessionPackageAndTreatmentPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PatientSessionPackages_TreatmentSchedulePlanId",
                schema: "package",
                table: "PatientSessionPackages",
                column: "TreatmentSchedulePlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientSessionPackages_TreatmentSchedulePlans_TreatmentSchedulePlanId",
                schema: "package",
                table: "PatientSessionPackages",
                column: "TreatmentSchedulePlanId",
                principalSchema: "package",
                principalTable: "TreatmentSchedulePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientSessionPackages_TreatmentSchedulePlans_TreatmentSchedulePlanId",
                schema: "package",
                table: "PatientSessionPackages");

            migrationBuilder.DropIndex(
                name: "IX_PatientSessionPackages_TreatmentSchedulePlanId",
                schema: "package",
                table: "PatientSessionPackages");
        }
    }
}
