using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableTreatmentSchedulePlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreatmentSchedulePlans",
                schema: "initialreport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    SessionDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionsPerWeek = table.Column<int>(type: "int", nullable: false),
                    MinimumGapBetweenSessionsDays = table.Column<int>(type: "int", nullable: false),
                    PreferredTimeOfDay = table.Column<int>(type: "int", nullable: false),
                    PreferredDays = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentSchedulePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentSchedulePlans_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreatmentSchedulePlans_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TreatmentSchedulePlans_InitialReport_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "initialreport",
                        principalTable: "InitialReport",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSchedulePlans_CreatedById",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSchedulePlans_ReportId",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                column: "ReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSchedulePlans_UpdatedById",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreatmentSchedulePlans",
                schema: "initialreport");
        }
    }
}
