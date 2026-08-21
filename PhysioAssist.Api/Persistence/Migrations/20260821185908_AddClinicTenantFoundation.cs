using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicTenantFoundation : Migration
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
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "Clinic",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinic", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patient_ClinicId_EmailAddress",
                schema: "patient",
                table: "Patient",
                columns: new[] { "ClinicId", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_ClinicId",
                table: "AspNetUsers",
                column: "ClinicId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Clinic_ClinicId",
                table: "AspNetUsers",
                column: "ClinicId",
                principalSchema: "auth",
                principalTable: "Clinic",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Patient_Clinic_ClinicId",
                schema: "patient",
                table: "Patient",
                column: "ClinicId",
                principalSchema: "auth",
                principalTable: "Clinic",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Clinic_ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Patient_Clinic_ClinicId",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropTable(
                name: "Clinic",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_Patient_ClinicId_EmailAddress",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUser_ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_EmailAddress",
                schema: "patient",
                table: "Patient",
                column: "EmailAddress",
                unique: true);
        }
    }
}
