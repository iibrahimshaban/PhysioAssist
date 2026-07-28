using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCopyTrackingToPatientFormSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CopyNumber",
                schema: "intake",
                table: "PatientFormSchema",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalFormId",
                schema: "intake",
                table: "PatientFormSchema",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalName",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientFormSchema_DoctorId_OriginalFormId",
                schema: "intake",
                table: "PatientFormSchema",
                columns: new[] { "DoctorId", "OriginalFormId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientFormSchema_DoctorId_OriginalFormId",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "CopyNumber",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "OriginalFormId",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "OriginalName",
                schema: "intake",
                table: "PatientFormSchema");
        }
    }
}
