using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableReceptionist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ManagingDoctorId1",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ManagingDoctorId1",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ManagingDoctorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ManagingDoctorId1",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "Receptionists",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    From = table.Column<DateTime>(type: "datetime2", nullable: true),
                    To = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Shift = table.Column<int>(type: "int", nullable: false),
                    ManagingDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receptionists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receptionists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Receptionists_Doctor_ManagingDoctorId",
                        column: x => x.ManagingDoctorId,
                        principalSchema: "auth",
                        principalTable: "Doctor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receptionists_ManagingDoctorId",
                schema: "auth",
                table: "Receptionists",
                column: "ManagingDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Receptionists_UserId",
                schema: "auth",
                table: "Receptionists",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Receptionists",
                schema: "auth");

            migrationBuilder.AddColumn<Guid>(
                name: "ManagingDoctorId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagingDoctorId1",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ManagingDoctorId1",
                table: "AspNetUsers",
                column: "ManagingDoctorId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ManagingDoctorId1",
                table: "AspNetUsers",
                column: "ManagingDoctorId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
