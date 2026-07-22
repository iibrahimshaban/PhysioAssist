using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentationModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Documentation");

            migrationBuilder.RenameColumn(
                name: "Summary",
                schema: "session",
                table: "Session",
                newName: "SummaryText");

            migrationBuilder.AddColumn<DateTime>(
                name: "SummaryGeneratedAt",
                schema: "session",
                table: "Session",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectiveFindings",
                schema: "initialreport",
                table: "InitialReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "patient",
                table: "DoctorPatient",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DocumentationSummaries",
                schema: "Documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: true),
                    FocusAreas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnonymizePersonalData = table.Column<bool>(type: "bit", nullable: false),
                    SummaryText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentationSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentationSummaries_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentationSummaries_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentationTemplates",
                schema: "Documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentationTemplates_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentationTemplates_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SessionProgressNotes",
                schema: "Documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subjective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectiveFindings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Assessment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionProgressNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionProgressNotes_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionProgressNotes_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionProgressNotes_DocumentationTemplates_DocumentationTemplateId",
                        column: x => x.DocumentationTemplateId,
                        principalSchema: "Documentation",
                        principalTable: "DocumentationTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentationSummaries_CreatedById",
                schema: "Documentation",
                table: "DocumentationSummaries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentationSummaries_UpdatedById",
                schema: "Documentation",
                table: "DocumentationSummaries",
                column: "UpdatedById");

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

            migrationBuilder.CreateIndex(
                name: "IX_SessionProgressNotes_CreatedById",
                schema: "Documentation",
                table: "SessionProgressNotes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SessionProgressNotes_DocumentationTemplateId",
                schema: "Documentation",
                table: "SessionProgressNotes",
                column: "DocumentationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionProgressNotes_UpdatedById",
                schema: "Documentation",
                table: "SessionProgressNotes",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentationSummaries",
                schema: "Documentation");

            migrationBuilder.DropTable(
                name: "SessionProgressNotes",
                schema: "Documentation");

            migrationBuilder.DropTable(
                name: "DocumentationTemplates",
                schema: "Documentation");

            migrationBuilder.DropColumn(
                name: "SummaryGeneratedAt",
                schema: "session",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "ObjectiveFindings",
                schema: "initialreport",
                table: "InitialReport");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "patient",
                table: "DoctorPatient");

            migrationBuilder.RenameColumn(
                name: "SummaryText",
                schema: "session",
                table: "Session",
                newName: "Summary");
        }
    }
}
