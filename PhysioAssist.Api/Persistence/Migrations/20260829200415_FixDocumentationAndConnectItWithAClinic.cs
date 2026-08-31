using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDocumentationAndConnectItWithAClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorDocumentationPreferences",
                schema: "Documentation");

            migrationBuilder.CreateTable(
                name: "ClinicDocumentationPreferences",
                schema: "Documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HiddenFieldIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDocumentationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicDocumentationPreferences_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClinicDocumentationPreferences_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClinicDocumentationPreferences_DocumentationTemplates_DocumentationTemplateId",
                        column: x => x.DocumentationTemplateId,
                        principalSchema: "Documentation",
                        principalTable: "DocumentationTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocumentationPreferences_ClinicId_DocumentationTemplateId",
                schema: "Documentation",
                table: "ClinicDocumentationPreferences",
                columns: new[] { "ClinicId", "DocumentationTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocumentationPreferences_CreatedById",
                schema: "Documentation",
                table: "ClinicDocumentationPreferences",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocumentationPreferences_DocumentationTemplateId",
                schema: "Documentation",
                table: "ClinicDocumentationPreferences",
                column: "DocumentationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocumentationPreferences_UpdatedById",
                schema: "Documentation",
                table: "ClinicDocumentationPreferences",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicDocumentationPreferences",
                schema: "Documentation");

            migrationBuilder.CreateTable(
                name: "DoctorDocumentationPreferences",
                schema: "Documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HiddenFieldIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
    }
}
