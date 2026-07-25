namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IAppointmentContactResolver
    {
        Task<(string Name, string Email)> GetPatientContactAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<string> GetDoctorNameAsync(Guid doctorId, CancellationToken cancellationToken = default);
    }
}
