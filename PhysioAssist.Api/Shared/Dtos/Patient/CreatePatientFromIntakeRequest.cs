namespace PhysioAssist.Api.Shared.Dtos.Patient;

public sealed record CreatePatientFromIntakeRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? Gender,
    DateTime? DateOfBirth,
    string? Occupation,
    Guid DoctorId,
    PatientCategory PatientCategory,
    string? FreeTime
    );
