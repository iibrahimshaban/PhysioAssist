namespace PhysioAssist.Api.Modules.PatientModule.DTOs
{
    public record CreateFromIntakeRequest(Guid FormSchemaId, string FormSubmissionData, string? PainPointsData);
}
