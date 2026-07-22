namespace PhysioAssist.Api.Shared.Dtos.Pdf;

public record TreatmentPlanPdfRequest(
    Guid ReportId,
    string PatientFullName,
    string DoctorFullName,
    string ReportText,
    DateTime ReportDate);
