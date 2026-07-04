using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class scheduleModuelv3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_WorkingSchedule_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart_Unique",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "SlotDurationMinutes",
                schema: "scheduling",
                table: "WorkingSchedule");

            migrationBuilder.DropColumn(
                name: "WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart_SlotEnd",
                schema: "scheduling",
                table: "ScheduleSlot",
                columns: new[] { "DoctorId", "SlotStart", "SlotEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_DoctorId_SlotStart_SlotEnd",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                schema: "scheduling",
                table: "WorkingSchedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_WorkingSchedule_WorkingScheduleId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "WorkingScheduleId",
                principalSchema: "scheduling",
                principalTable: "WorkingSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
