using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicScopeToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkingSchedule_DoctorId_ActiveOnly",
                schema: "scheduling",
                table: "WorkingSchedule");

            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                schema: "scheduling",
                table: "WorkingSchedule",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                schema: "scheduling",
                table: "WorkingSchedule",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedule_ClinicId_ActiveDefaultOnly",
                schema: "scheduling",
                table: "WorkingSchedule",
                column: "ClinicId",
                unique: true,
                filter: "[IsActive] = 1 AND [DoctorId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedule_DoctorId_ActiveOverrideOnly",
                schema: "scheduling",
                table: "WorkingSchedule",
                column: "DoctorId",
                unique: true,
                filter: "[IsActive] = 1 AND [DoctorId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkingSchedule_ClinicId_ActiveDefaultOnly",
                schema: "scheduling",
                table: "WorkingSchedule");

            migrationBuilder.DropIndex(
                name: "IX_WorkingSchedule_DoctorId_ActiveOverrideOnly",
                schema: "scheduling",
                table: "WorkingSchedule");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                schema: "scheduling",
                table: "WorkingSchedule");

            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                schema: "scheduling",
                table: "WorkingSchedule",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkingSchedule_DoctorId_ActiveOnly",
                schema: "scheduling",
                table: "WorkingSchedule",
                column: "DoctorId",
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
