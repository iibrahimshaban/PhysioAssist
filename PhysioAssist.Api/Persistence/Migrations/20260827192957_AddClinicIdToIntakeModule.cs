using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicIdToIntakeModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DoctorId",
                schema: "intake",
                table: "PreVisitIntake",
                newName: "GeneratedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_PreVisitIntake_DoctorId_Status_SubmittedAt",
                schema: "intake",
                table: "PreVisitIntake",
                newName: "IX_PreVisitIntake_GeneratedByUserId_Status_SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "DoctorId",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "ClinicId");

            migrationBuilder.RenameIndex(
                name: "IX_PatientFormSchema_DoctorId_Status",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "IX_PatientFormSchema_ClinicId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_PatientFormSchema_DoctorId_Name",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "IX_PatientFormSchema_ClinicId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_PatientFormSchema_DoctorId_IsDefault",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "IX_PatientFormSchema_ClinicId_IsDefault");

            migrationBuilder.CreateTable(
                name: "IntakeFormAccess",
                schema: "intake",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nonce = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeFormAccess", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntakeFormAccess",
                schema: "intake");

            migrationBuilder.RenameColumn(
                name: "GeneratedByUserId",
                schema: "intake",
                table: "PreVisitIntake",
                newName: "DoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_PreVisitIntake_GeneratedByUserId_Status_SubmittedAt",
                schema: "intake",
                table: "PreVisitIntake",
                newName: "IX_PreVisitIntake_DoctorId_Status_SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "ClinicId",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "DoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_PatientFormSchema_ClinicId_Status",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "IX_PatientFormSchema_DoctorId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_PatientFormSchema_ClinicId_Name",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "IX_PatientFormSchema_DoctorId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_PatientFormSchema_ClinicId_IsDefault",
                schema: "intake",
                table: "PatientFormSchema",
                newName: "IX_PatientFormSchema_DoctorId_IsDefault");
        }
    }
}
