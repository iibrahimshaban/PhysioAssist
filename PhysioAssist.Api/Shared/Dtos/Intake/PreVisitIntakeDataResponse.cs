namespace PhysioAssist.Api.Shared.Dtos.Intake;

public record PreVisitIntakeDataResponse(
    Guid Id,
    Guid DoctorId,
    Guid FormSchemaId,
    int FormSchemaVersion,
    string FormSubmissionData,
    string? PainPointsData,
    IntakeStatus Status,
    Guid? ConvertedToPatientId,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByDoctorId
    );
