using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "GuestId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Guest",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guest", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_GuestId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "GuestId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ScheduleSlot_ExactlyOneOwner",
                schema: "scheduling",
                table: "ScheduleSlot",
                sql: "([PatientId] IS NOT NULL AND [GuestId] IS NULL) OR ([PatientId] IS NULL AND [GuestId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_Guest_GuestId",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "GuestId",
                principalSchema: "scheduling",
                principalTable: "Guest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_Guest_GuestId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropTable(
                name: "Guest",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_GuestId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ScheduleSlot_ExactlyOneOwner",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "GuestId",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
