using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuditbleLoggingFromDocumentationTemplateTabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentationTemplates_AspNetUsers_CreatedById",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentationTemplates_AspNetUsers_UpdatedById",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DocumentationTemplates_CreatedById",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DocumentationTemplates_UpdatedById",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                schema: "Documentation",
                table: "DocumentationTemplates");

            migrationBuilder.CreateTable(
                name: "DoctorDocumentationPreferences",
                schema: "Documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HiddenFieldIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorDocumentationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorDocumentationPreferences_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorDocumentationPreferences_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DoctorDocumentationPreferences_DocumentationTemplates_DocumentationTemplateId",
                        column: x => x.DocumentationTemplateId,
                        principalSchema: "Documentation",
                        principalTable: "DocumentationTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorDocumentationPreferences_CreatedById",
                schema: "Documentation",
                table: "DoctorDocumentationPreferences",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorDocumentationPreferences_DoctorId_DocumentationTemplateId",
                schema: "Documentation",
                table: "DoctorDocumentationPreferences",
                columns: new[] { "DoctorId", "DocumentationTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorDocumentationPreferences_DocumentationTemplateId",
                schema: "Documentation",
                table: "DoctorDocumentationPreferences",
                column: "DocumentationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorDocumentationPreferences_UpdatedById",
                schema: "Documentation",
                table: "DoctorDocumentationPreferences",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorDocumentationPreferences",
                schema: "Documentation");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "Documentation",
                table: "DocumentationTemplates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                schema: "Documentation",
                table: "DocumentationTemplates",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Documentation",
                table: "DocumentationTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                schema: "Documentation",
                table: "DocumentationTemplates",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentationTemplates_CreatedById",
                schema: "Documentation",
                table: "DocumentationTemplates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentationTemplates_UpdatedById",
                schema: "Documentation",
                table: "DocumentationTemplates",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentationTemplates_AspNetUsers_CreatedById",
                schema: "Documentation",
                table: "DocumentationTemplates",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentationTemplates_AspNetUsers_UpdatedById",
                schema: "Documentation",
                table: "DocumentationTemplates",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
