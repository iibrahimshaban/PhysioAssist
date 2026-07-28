namespace PhysioAssist.Api.Modules.Intake.QueryServices
{
    public interface IIntakeCreationQueryService
    {
        Task<Result<Guid>> CreateDirectIntakeAsync(Guid formSchemaId, string formSubmissionData, string? painPointsData, Guid doctorId, CancellationToken ct = default);
    }
}
