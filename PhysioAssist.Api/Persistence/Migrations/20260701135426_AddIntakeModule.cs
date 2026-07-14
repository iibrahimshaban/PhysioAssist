using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhysioAssist.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                schema: "intake",
                table: "PreVisitIntake",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "intake",
                table: "PreVisitIntake",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PatientName",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "PainPointsData",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AccessTokenHash",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "intake",
                table: "PreVisitIntake",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "FormSchemaVersion",
                schema: "intake",
                table: "PreVisitIntake",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PatientEmail",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientPhone",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "intake",
                table: "PreVisitIntake",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByDoctorId",
                schema: "intake",
                table: "PreVisitIntake",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDefault",
                schema: "intake",
                table: "PatientFormSchema",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                schema: "intake",
                table: "PatientFormSchema",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchemaHash",
                schema: "intake",
                table: "PatientFormSchema",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "intake",
                table: "PatientFormSchema",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "intake",
                table: "PatientFormSchema",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_PreVisitIntake_AccessTokenHash",
                schema: "intake",
                table: "PreVisitIntake",
                column: "AccessTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_PreVisitIntake_ConvertedToPatientId",
                schema: "intake",
                table: "PreVisitIntake",
                column: "ConvertedToPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVisitIntake_DoctorId_Status_SubmittedAt",
                schema: "intake",
                table: "PreVisitIntake",
                columns: new[] { "DoctorId", "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PreVisitIntake_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake",
                column: "FormSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFormSchema_DoctorId_IsDefault",
                schema: "intake",
                table: "PatientFormSchema",
                columns: new[] { "DoctorId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientFormSchema_DoctorId_Name",
                schema: "intake",
                table: "PatientFormSchema",
                columns: new[] { "DoctorId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientFormSchema_DoctorId_Status",
                schema: "intake",
                table: "PatientFormSchema",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake",
                column: "FormSchemaId",
                principalSchema: "intake",
                principalTable: "PatientFormSchema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreVisitIntake_PatientFormSchema_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropIndex(
                name: "IX_PreVisitIntake_AccessTokenHash",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropIndex(
                name: "IX_PreVisitIntake_ConvertedToPatientId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropIndex(
                name: "IX_PreVisitIntake_DoctorId_Status_SubmittedAt",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropIndex(
                name: "IX_PreVisitIntake_FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropIndex(
                name: "IX_PatientFormSchema_DoctorId_IsDefault",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropIndex(
                name: "IX_PatientFormSchema_DoctorId_Name",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropIndex(
                name: "IX_PatientFormSchema_DoctorId_Status",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "AccessTokenHash",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "FormSchemaId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "FormSchemaVersion",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "PatientEmail",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "PatientPhone",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "ReviewedByDoctorId",
                schema: "intake",
                table: "PreVisitIntake");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "SchemaHash",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "intake",
                table: "PatientFormSchema");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                schema: "intake",
                table: "PreVisitIntake",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "intake",
                table: "PreVisitIntake",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PatientName",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "PainPointsData",
                schema: "intake",
                table: "PreVisitIntake",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDefault",
                schema: "intake",
                table: "PatientFormSchema",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }
    }
}
