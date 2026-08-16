using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSameDayBookingToTreatmentPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowSameDayBooking",
                schema: "initialreport",
                table: "TreatmentSchedulePlans",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowSameDayBooking",
                schema: "initialreport",
                table: "TreatmentSchedulePlans");
        }
    }
}
