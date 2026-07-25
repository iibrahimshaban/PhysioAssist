using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeTimeColumnsToTablePatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParsedPreferredDayToken",
                schema: "patient",
                table: "Patient",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ParsedPreferredTimeFrom",
                schema: "patient",
                table: "Patient",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ParsedPreferredTimeTo",
                schema: "patient",
                table: "Patient",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientFreeTime",
                schema: "patient",
                table: "Patient",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParsedPreferredDayToken",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ParsedPreferredTimeFrom",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ParsedPreferredTimeTo",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "PatientFreeTime",
                schema: "patient",
                table: "Patient");
        }
    }
}
