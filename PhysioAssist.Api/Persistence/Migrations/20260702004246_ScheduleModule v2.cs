using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleModulev2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkingScheduleDay_WorkingScheduleId",
                table: "WorkingScheduleDay");

            migrationBuilder.RenameTable(
                name: "WorkingScheduleDay",
                newName: "WorkingScheduleDay",
                newSchema: "scheduling");

            migrationBuilder.RenameTable(
                name: "WorkingSchedule",
                newName: "WorkingSchedule",
                newSchema: "scheduling");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingScheduleDay_Schedule_Day_Unique",
                schema: "scheduling",
                table: "WorkingScheduleDay",
                columns: new[] { "WorkingScheduleId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedule_DoctorId_ActiveOnly",
                schema: "scheduling",
                table: "WorkingSchedule",
                column: "DoctorId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkingScheduleDay_Schedule_Day_Unique",
                schema: "scheduling",
                table: "WorkingScheduleDay");

            migrationBuilder.DropIndex(
                name: "IX_WorkingSchedule_DoctorId_ActiveOnly",
                schema: "scheduling",
                table: "WorkingSchedule");

            migrationBuilder.RenameTable(
                name: "WorkingScheduleDay",
                schema: "scheduling",
                newName: "WorkingScheduleDay");

            migrationBuilder.RenameTable(
                name: "WorkingSchedule",
                schema: "scheduling",
                newName: "WorkingSchedule");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingScheduleDay_WorkingScheduleId",
                table: "WorkingScheduleDay",
                column: "WorkingScheduleId");
        }
    }
}
