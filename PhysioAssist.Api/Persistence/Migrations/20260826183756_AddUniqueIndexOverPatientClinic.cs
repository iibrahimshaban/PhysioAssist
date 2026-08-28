using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOverPatientClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patient_EmailAddress",
                schema: "patient",
                table: "Patient");

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                schema: "patient",
                table: "Patient",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Patient_ClinicId_EmailAddress",
                schema: "patient",
                table: "Patient",
                columns: new[] { "ClinicId", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patient_ClinicId_PhoneNumber",
                schema: "patient",
                table: "Patient",
                columns: new[] { "ClinicId", "PhoneNumber" },
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patient_ClinicId_EmailAddress",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropIndex(
                name: "IX_Patient_ClinicId_PhoneNumber",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                schema: "patient",
                table: "Patient");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_EmailAddress",
                schema: "patient",
                table: "Patient",
                column: "EmailAddress",
                unique: true);
        }
    }
}
