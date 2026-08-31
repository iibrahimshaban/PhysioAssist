using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_PatientSessionPackages_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentSchedulePlans_InitialReport_ReportId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentSchedulePlans_ReportId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans");

            migrationBuilder.DropColumn(
                name: "PackageId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans");

            migrationBuilder.EnsureSchema(
                name: "package");

            migrationBuilder.RenameTable(
                name: "TreatmentSchedulePlans",
                schema: "initialreport",
                newName: "TreatmentSchedulePlans",
                newSchema: "package");

            migrationBuilder.RenameTable(
                name: "PatientSessionPackages",
                schema: "scheduling",
                newName: "PatientSessionPackages",
                newSchema: "package");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "package",
                table: "TreatmentSchedulePlans",
                newName: "LifeCycleStatus");

            migrationBuilder.AddColumn<Guid>(
                name: "TreatmentSchedulePlanId",
                schema: "package",
                table: "PatientSessionPackages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PackageDoctorAssignmentHistories",
                schema: "package",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDoctorAssignmentHistories", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_PackageDoctorAssignmentHistories_PatientSessionPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "package",
                        principalTable: "PatientSessionPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDoctorAssignmentHistories_PackageId",
                schema: "package",
                table: "PackageDoctorAssignmentHistories",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDoctorAssignmentHistories_PackageId_ChangedAt",
                schema: "package",
                table: "PackageDoctorAssignmentHistories",
                columns: new[] { "PackageId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDoctorAssignmentHistories_SessionId",
                schema: "package",
                table: "PackageDoctorAssignmentHistories",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageDoctorAssignmentHistories",
                schema: "package");

            migrationBuilder.DropColumn(
                name: "TreatmentSchedulePlanId",
                schema: "package",
                table: "PatientSessionPackages");

            migrationBuilder.RenameTable(
                name: "TreatmentSchedulePlans",
                schema: "package",
                newName: "TreatmentSchedulePlans",
                newSchema: "initialreport");

            migrationBuilder.RenameTable(
                name: "PatientSessionPackages",
                schema: "package",
                newName: "PatientSessionPackages",
                newSchema: "scheduling");

            migrationBuilder.RenameColumn(
                name: "LifeCycleStatus",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                newName: "Status");

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSchedulePlans_ReportId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                column: "ReportId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_PatientSessionPackages_PackageId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "PackageId",
                principalSchema: "scheduling",
                principalTable: "PatientSessionPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentSchedulePlans_InitialReport_ReportId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                column: "ReportId",
                principalSchema: "initialreport",
                principalTable: "InitialReport",
                principalColumn: "Id");
        }
    }
}
