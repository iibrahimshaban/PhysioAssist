namespace PhysioAssist.Api.Modules.Intake.QueryServices
{
    public interface IIntakeConversionMarkerService
    {
        Task<Result> MarkIntakeConvertedAsync(Guid intakeId, Guid patientId, Guid doctorId, CancellationToken ct = default);
    }
}
