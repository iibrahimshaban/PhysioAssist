using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Modules.Intake.Errors;
using PhysioAssist.Api.Modules.Intake.Helpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.Intake.QueryServices;

public class IntakeCreationQueryService(ApplicationDbContext context) : IIntakeCreationQueryService, IIntakeConversionMarkerService
{
    private readonly ApplicationDbContext _context = context;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<Guid>> CreateDirectIntakeAsync(Guid formSchemaId, string formSubmissionData, string? painPointsData, Guid doctorId, CancellationToken ct = default)
    {
        var schema = await _context.PatientFormSchemas
            .FirstOrDefaultAsync(s => s.Id == formSchemaId, ct);

        if (schema is null)
            return Result.Failure<Guid>(IntakeErrors.SchemaNotFound);

        if (schema.DoctorId != doctorId)
            return Result.Failure<Guid>(IntakeErrors.UnauthorizedDoctor);

        var schemaDto = DeserializeSchemaJson(schema.SchemaJson);
        if (schemaDto is null)
            return Result.Failure<Guid>(IntakeErrors.InvalidSchema);

        var submissionDto = ExtractInputValuesHelper.DeserializeSubmissionJson(formSubmissionData);
        if (submissionDto is null)
            return Result.Failure<Guid>(IntakeErrors.InvalidSubmission);

        var intake = new PreVisitIntake
        {
            DoctorId = doctorId,
            FormSchemaId = schema.Id,
            FormSchemaVersion = schema.Version,
            FormSubmissionData = formSubmissionData,
            PainPointsData = painPointsData,
            Status = IntakeStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            ShortCode = await GenerateUniqueFormShortCodeAsync(ct),
        };

        _context.PreVisitIntakes.Add(intake);
        await _context.SaveChangesAsync(ct);

        return Result.Success(intake.Id);
    }

    public async Task<Result> MarkIntakeConvertedAsync(Guid intakeId, Guid patientId, Guid doctorId, CancellationToken ct = default)
    {
        var intake = await _context.PreVisitIntakes.FirstOrDefaultAsync(i => i.Id == intakeId, ct);
        if (intake is null)
            return Result.Failure(IntakeErrors.IntakeNotFound);

        intake.ConvertedToPatientId = patientId;
        intake.Status = IntakeStatus.Converted;
        intake.ReviewedAt = DateTime.UtcNow;
        intake.ReviewedByDoctorId = doctorId;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private DynamicFormSchemaDto? DeserializeSchemaJson(string schemaJson)
    {
        try { return JsonSerializer.Deserialize<DynamicFormSchemaDto>(schemaJson, _jsonOptions); }
        catch (JsonException) { return null; }
    }
    private async Task<string> GenerateUniqueFormShortCodeAsync(CancellationToken cancellationToken)
    {
        string? shortCode;
        do
        {
            shortCode = GenerateShortCode();
        } while (await _context.PatientFormSchemas.AnyAsync(s => s.ShortCode == shortCode, cancellationToken));
        return shortCode;
    }
    private static string GenerateShortCode(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        byte[] data = new byte[length];
        rng.GetBytes(data);
        StringBuilder result = new(length);
        foreach (byte b in data)
        {
            result.Append(chars[b % chars.Length]);
        }
        return result.ToString();
    }
}