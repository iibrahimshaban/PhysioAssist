using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendScheduleModuleToAddShceduleAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorSchedulingPreferences",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxShortfallTolerance = table.Column<TimeSpan>(type: "time", nullable: false),
                    MaxDaysOutForExactMatch = table.Column<int>(type: "int", nullable: false),
                    AllowShorterSlots = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedulingPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientSessionPackages",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    ScheduledSessions = table.Column<int>(type: "int", nullable: false),
                    RemainingSessions = table.Column<int>(type: "int", nullable: false),
                    SessionDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientSessionPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientSessionPackages_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientSessionPackages_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedulingPreferences_DoctorId",
                schema: "scheduling",
                table: "DoctorSchedulingPreferences",
                column: "DoctorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientSessionPackages_CreatedById",
                schema: "scheduling",
                table: "PatientSessionPackages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PatientSessionPackages_DoctorId_Status",
                schema: "scheduling",
                table: "PatientSessionPackages",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientSessionPackages_PatientId_Status",
                schema: "scheduling",
                table: "PatientSessionPackages",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientSessionPackages_UpdatedById",
                schema: "scheduling",
                table: "PatientSessionPackages",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_PatientSessionPackages_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "PackageId",
                principalSchema: "scheduling",
                principalTable: "PatientSessionPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_PatientSessionPackages_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropTable(
                name: "DoctorSchedulingPreferences",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "PatientSessionPackages",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "PackageId",
                schema: "scheduling",
                table: "ScheduleSlot");
        }
    }
}
