using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendScheduleAgentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumGapBetweenSessionsDays",
                schema: "scheduling",
                table: "PatientSessionPackages",
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

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "scheduling",
                table: "PatientSessionPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionsPerWeek",
                schema: "scheduling",
                table: "PatientSessionPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumGapBetweenSessionsDays",
                schema: "scheduling",
                table: "PatientSessionPackages");

            migrationBuilder.DropColumn(
                name: "PreferredDays",
                schema: "scheduling",
                table: "PatientSessionPackages");

            migrationBuilder.DropColumn(
                name: "PreferredTimeOfDay",
                schema: "scheduling",
                table: "PatientSessionPackages");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "scheduling",
                table: "PatientSessionPackages");

            migrationBuilder.DropColumn(
                name: "SessionsPerWeek",
                schema: "scheduling",
                table: "PatientSessionPackages");
        }
    }
}
