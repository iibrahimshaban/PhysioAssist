using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Schedualv13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_AspNetUsers_CreatedById",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSlot_AspNetUsers_UpdatedById",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_CreatedById",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlot_UpdatedById",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "scheduling",
                table: "ScheduleSlot");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                schema: "scheduling",
                table: "ScheduleSlot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                schema: "scheduling",
                table: "ScheduleSlot",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_CreatedById",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlot_UpdatedById",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_AspNetUsers_CreatedById",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSlot_AspNetUsers_UpdatedById",
                schema: "scheduling",
                table: "ScheduleSlot",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
