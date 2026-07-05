using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleModulev1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkingSchedule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingSchedule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkingScheduleDay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkingScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingScheduleDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkingScheduleDay_WorkingSchedule_WorkingScheduleId",
                        column: x => x.WorkingScheduleId,
                        principalTable: "WorkingSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart_Unique",
                schema: "scheduling",
                table: "ScheduleSlot",
                columns: new[] { "DoctorId", "SlotStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "WorkingScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingScheduleDay_WorkingScheduleId",
                table: "WorkingScheduleDay",
                column: "WorkingScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_WorkingSchedule_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "WorkingScheduleId",
                principalTable: "WorkingSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_WorkingSchedule_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropTable(
                name: "WorkingScheduleDay");

            migrationBuilder.DropTable(
                name: "WorkingSchedule");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart_Unique",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart",
                schema: "scheduling",
                table: "ScheduleSlot",
                columns: new[] { "DoctorId", "SlotStart" });
        }
    }
}
