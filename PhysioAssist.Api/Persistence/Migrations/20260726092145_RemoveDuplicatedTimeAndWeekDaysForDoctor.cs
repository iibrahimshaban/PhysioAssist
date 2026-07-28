using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicatedTimeAndWeekDaysForDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredDays",
                schema: "initialreport",
                table: "TreatmentSchedulePlans");

            migrationBuilder.DropColumn(
                name: "PreferredTimeOfDay",
                schema: "initialreport",
                table: "TreatmentSchedulePlans");

            migrationBuilder.DropColumn(
                name: "PreferredDays",
                schema: "scheduling",
                table: "PatientSessionPackages");

            migrationBuilder.DropColumn(
                name: "PreferredTimeOfDay",
                schema: "scheduling",
                table: "PatientSessionPackages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredDays",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredTimeOfDay",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredDays",
                schema: "scheduling",
                table: "PatientSessionPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredTimeOfDay",
                schema: "scheduling",
                table: "PatientSessionPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
