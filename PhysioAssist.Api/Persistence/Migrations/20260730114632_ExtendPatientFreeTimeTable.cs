using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPatientFreeTimeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParsedPreferredTimeFrom",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ParsedPreferredTimeTo",
                schema: "patient",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ParsedPreferredWeekdays",
                schema: "patient",
                table: "Patient");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ParsedPreferredExplicitDate",
                schema: "patient",
                table: "Patient",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PatientPreferredTimeSlot",
                schema: "patient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weekdays = table.Column<int>(type: "int", nullable: false),
                    TimeFrom = table.Column<TimeOnly>(type: "time", nullable: true),
                    TimeTo = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientPreferredTimeSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientPreferredTimeSlot_Patient_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patient",
                        principalTable: "Patient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientPreferredTimeSlot_PatientId",
                schema: "patient",
                table: "PatientPreferredTimeSlot",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientPreferredTimeSlot",
                schema: "patient");

            migrationBuilder.DropColumn(
                name: "ParsedPreferredExplicitDate",
                schema: "patient",
                table: "Patient");

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

            migrationBuilder.AddColumn<int>(
                name: "ParsedPreferredWeekdays",
                schema: "patient",
                table: "Patient",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
